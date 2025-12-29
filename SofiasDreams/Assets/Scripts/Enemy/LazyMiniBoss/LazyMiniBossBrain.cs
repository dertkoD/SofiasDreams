using UnityEngine;
using Zenject;

public class LazyMiniBossBrain : MonoBehaviour
{
    enum State
    {
        Patrol,
        TriggerAgro,
        Agro,
        AttackMelee,
        AttackShoot,
        TriggerPatrol,
        Death
    }

    [Header("Refs")]
    [SerializeField] LazyMiniBossMotor2D _motor;
    [SerializeField] LazyMiniBossAnimatorAdapter _anim;
    [SerializeField] VisionCone2D _vision;
    [SerializeField] Health _health;
    [SerializeField] EnemyPatrolPath _patrolPath;
    [SerializeField] Transform _projectileSpawnPoint;

    LazyMiniBossConfigSO _config;
    IHealth _iHealth;
    SignalBus _bus;
    Transform _player;
    
    State _state;
    EnemyPatrolPath _path;
    int _pathIndex;
    int _pathDir = 1;

    // Agro
    float _forgetTimer;
    bool _hasSeenPlayer;
    Vector2 _lastSeenPos;

    // Combat
    float _nextMeleeAttackTime;
    float _nextShootAttackTime;
    
    // Internal flags for melee sequence
    bool _attack1Triggered;
    bool _attack2Triggered;

    [Inject]
    public void Construct(LazyMiniBossConfigSO config, IHealth health, SignalBus bus)
    {
        _config = config;
        _iHealth = health;
        _bus = bus;
    }

    void Awake()
    {
        if (!_motor) _motor = GetComponent<LazyMiniBossMotor2D>();
        if (!_anim) _anim = GetComponent<LazyMiniBossAnimatorAdapter>();
        if (!_vision) _vision = GetComponentInChildren<VisionCone2D>();
        if (!_health) _health = GetComponent<Health>();
        
        if (_iHealth == null) _iHealth = _health as IHealth;
        
        if (!_patrolPath) _patrolPath = FindNearestPatrolPath();
        _path = _patrolPath;
    }

    void Start()
    {
        _state = State.Patrol;
        if (_path != null && _path.Count > 0)
        {
            _pathIndex = FindNearestWaypointIndex(transform.position);
        }
        
        // Try find player
         var pf = FindObjectOfType<PlayerFacade>();
         if (pf != null) _player = pf.transform;
    }

    void Update()
    {
        if (!_iHealth.IsAlive)
        {
            if (_state != State.Death) EnterDeath();
            return;
        }

        bool seesPlayer = TrySense(out Transform target);
        if (seesPlayer)
        {
            _player = target;
            _lastSeenPos = target.position;
            _hasSeenPlayer = true;
            _forgetTimer = _config.agroForgetSeconds;
        }
        else
        {
            if (_forgetTimer > 0) _forgetTimer -= Time.deltaTime;
        }

        switch (_state)
        {
            case State.Patrol:
                if (seesPlayer) EnterTriggerAgro();
                else TickPatrol();
                break;

            case State.TriggerAgro:
                if (_anim.IsInAgroMovement())
                {
                    _state = State.Agro;
                }
                break;

            case State.Agro:
                TickAgro(seesPlayer);
                break;

            case State.TriggerPatrol:
                if (_anim.IsInPatrolMovement())
                {
                    _state = State.Patrol;
                    _pathIndex = FindNearestWaypointIndex(transform.position);
                }
                break;
                
            case State.AttackMelee:
                TickAttackMelee();
                break;
                
            case State.AttackShoot:
                TickAttackShoot();
                break;
        }
        
        UpdateAnimator();
    }

    void EnterDeath()
    {
        _state = State.Death;
        _motor.Stop();
        _anim.TriggerDeath();
        enabled = false;
    }

    void EnterTriggerAgro()
    {
        _state = State.TriggerAgro;
        _motor.Stop();
        _anim.TriggerAgro();
    }

    void EnterTriggerPatrol()
    {
        _state = State.TriggerPatrol;
        _motor.Stop();
        _anim.TriggerPatrol();
        _hasSeenPlayer = false;
    }
    
    void TickPatrol()
    {
        if (_path == null || _path.Count == 0) return;

        Vector3 target = _path.GetPoint(_pathIndex);
        float dist = Vector2.Distance(transform.position, target);
        
        if (dist <= _config.waypointArriveDistance)
        {
            AdvancePathIndex();
            // Wait logic could go here
        }

        // Move towards target
        float dx = target.x - transform.position.x;
        _motor.Move(Mathf.Sign(dx) * _config.patrolSpeed);
    }

    void TickAgro(bool seesPlayer)
    {
        if (_forgetTimer <= 0 && !seesPlayer)
        {
            EnterTriggerPatrol();
            return;
        }

        Vector3 targetPos = seesPlayer ? _player.position : (Vector3)_lastSeenPos;
        float dist = Vector2.Distance(transform.position, targetPos);
        float dx = targetPos.x - transform.position.x;
        
        // Face the target
        if (Mathf.Abs(dx) > 0.1f) _motor.Face(dx > 0 ? 1 : -1);

        // Combat Logic
        if (dist <= _config.closeRangeThreshold)
        {
             // Melee Range
             if (Time.time >= _nextMeleeAttackTime)
             {
                 StartMeleeAttack();
                 return;
             }
        }
        else if (seesPlayer && dist >= _config.shootRangeMin) // Shoot Range
        {
            if (Time.time >= _nextShootAttackTime)
            {
                StartShootAttack();
                return;
            }
        }

        // Move towards target if not attacking
        if (dist > _config.closeRangeThreshold * 0.8f) // Keep some distance? Or close in?
        {
             // If we are far, run towards
             // If we are in shoot range, maybe stop to shoot?
             // Assuming chase logic:
             _motor.Move(Mathf.Sign(dx) * _config.agroRunSpeed);
        }
        else
        {
            _motor.Stop();
        }
    }

    void StartMeleeAttack()
    {
        _state = State.AttackMelee;
        _motor.Stop();
        _attack1Triggered = true;
        _attack2Triggered = false;
        _anim.SetAttack1(true);
        _nextMeleeAttackTime = Time.time + _config.meleeAttackCooldown;
    }

    void TickAttackMelee()
    {
        // Simple sequencer
        // If in Attack1 and haven't triggered Attack2 yet -> Trigger Attack2
        if (_anim.IsInAttack1() && !_attack2Triggered)
        {
             _anim.SetAttack1(false); // Reset boolean
             _anim.SetAttack2(true);
             _attack2Triggered = true;
        }
        
        if (_anim.IsInAttack2())
        {
            _anim.SetAttack2(false); // Reset boolean
        }

        // Check for exit
        if (_anim.IsInAgroMovement() && _attack2Triggered) 
        {
             _state = State.Agro;
        }
    }

    void StartShootAttack()
    {
        _state = State.AttackShoot;
        _motor.Stop();
        _anim.TriggerShoot();
        // Spawn projectile immediately or delay?
        // Let's spawn with a slight delay or now. 
        // Ideally we use animation event.
        // I will invoke a method after 0.2s as a fallback.
        Invoke(nameof(SpawnProjectile), 0.2f);
        
        _nextShootAttackTime = Time.time + _config.shootAttackCooldown;
    }

    void TickAttackShoot()
    {
        if (_anim.IsInAgroMovement())
        {
            _state = State.Agro;
        }
    }

    void SpawnProjectile()
    {
        if (_state != State.AttackShoot) return; // Cancel if interrupted
        
        if (_config.projectilePrefab)
        {
            Vector3 spawnPos = _projectileSpawnPoint ? _projectileSpawnPoint.position : transform.position;
            GameObject go = Instantiate(_config.projectilePrefab, spawnPos, Quaternion.identity);
            var proj = go.GetComponent<FistProjectile>();
            
            // Determine direction
            int dir = _motor.IsFacingRight ? 1 : -1;
            Vector2 direction = new Vector2(dir, 0);
            
            // If player is known, aim at player? Or just forward?
            // "Shooting projectile" - usually linear.
            if (_player)
            {
                Vector2 diff = _player.position - spawnPos;
                if (diff.x * dir > 0) // only if in front
                {
                   direction = diff.normalized;
                }
            }
            
            if (proj) 
            {
                proj.Setup(_config.projectileDamage);
                proj.Fire(direction, _config.projectileSpeed);
            }
        }
    }

    void UpdateAnimator()
    {
        if (_state == State.Patrol)
        {
            // Set xVelocity based on motor velocity
            _anim.SetXVelocity(Mathf.Abs(_motor.Velocity.x));
        }
        else if (_state == State.Agro)
        {
            _anim.SetXVelocity(Mathf.Abs(_motor.Velocity.x));
        }
    }

    bool TrySense(out Transform target)
    {
        target = null;
        if (_vision == null) return false;
        return _vision.TryGetClosestTarget(out target);
    }
    
    // Path Utilities
    EnemyPatrolPath FindNearestPatrolPath()
    {
        var all = FindObjectsOfType<EnemyPatrolPath>();
        if (all == null || all.Length == 0) return null;

        float best = float.PositiveInfinity;
        EnemyPatrolPath bestPath = null;
        Vector2 pos = transform.position;

        foreach(var p in all)
        {
            if (p == null || p.Count == 0) continue;
            float d = Vector2.Distance(pos, p.transform.position);
            if (d < best)
            {
                best = d;
                bestPath = p;
            }
        }
        return bestPath;
    }
    
    int FindNearestWaypointIndex(Vector2 pos)
    {
        if (_path == null || _path.Count == 0) return 0;
        int bestIndex = 0;
        float best = float.PositiveInfinity;
        for (int i = 0; i < _path.Count; i++)
        {
            Vector2 p = _path.GetPoint(i);
            float d = (p - pos).sqrMagnitude;
            if (d < best) { best = d; bestIndex = i; }
        }
        return bestIndex;
    }
    
    void AdvancePathIndex()
    {
        if (_path == null || _path.Count <= 1) return;
        if (_config != null && _config.loopPath)
        {
            _pathIndex = (_pathIndex + 1) % _path.Count;
            return;
        }

        int next = _pathIndex + _pathDir;
        if (next >= _path.Count || next < 0)
        {
            _pathDir *= -1;
            next = Mathf.Clamp(_pathIndex + _pathDir, 0, _path.Count - 1);
        }
        _pathIndex = next;
    }
}
