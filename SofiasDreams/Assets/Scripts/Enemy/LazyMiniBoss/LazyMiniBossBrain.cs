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
    [SerializeField] LazyMiniBossConfigSO _config;

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
    bool _hasPerformedFirstMelee; // New flag
    
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
        _hasPerformedFirstMelee = false; // Reset on lost agro? Or keep it? Usually reset means "new encounter"
        // If we want him to ALWAYS do melee first on every new encounter, reset it.
    }
    
    void TickPatrol()
    {
        if (_path == null || _path.Count == 0) return;

        Vector3 target = _path.GetPoint(_pathIndex);
        
        // 1. Calculate distance purely on X if it's a 2D platformer on flat ground, 
        // or ensure the Y difference doesn't block arrival if waypoints are slightly off.
        // For simplicity, let's stick to Distance but ensure we don't overshoot or get stuck.
        float dist = Vector2.Distance(transform.position, target);
        
        // Check if we reached it
        if (dist <= _config.waypointArriveDistance)
        {
            AdvancePathIndex();
            // Important: if we just advanced, we should check if we should wait or immediately move to next.
            // For now, immediately move to next in next frame.
            _motor.Stop(); // Stop momentarily
            return;
        }

        // Move towards target
        float dx = target.x - transform.position.x;
        
        // Fix: If we are very close on X but Y is different (e.g. waypoint slightly in air/ground),
        // we might oscillate. 
        if (Mathf.Abs(dx) < 0.05f) 
        {
             // We are at the X position but maybe Y is off. Consider arrived if Y difference is small?
             // Or just force advance.
             // If we are this close in X, but Distance check failed, it means Y is off.
             // Let's rely on distance check mostly, but if X is aligned, maybe we are stuck?
             // Let's trust distance but ensure waypoints are placed well.
             
             // HOWEVER, if the enemy overshoots, he might turn back.
             // To prevent "stuck at point 1", ensure AdvancePathIndex logic is correct.
        }

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
        else if (seesPlayer && dist >= _config.shootRangeMin && _hasPerformedFirstMelee) // Shoot Range only after first melee
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
             // If we haven't done first melee, we MUST close in
             bool forceCloseIn = !_hasPerformedFirstMelee;
             
             // If we are far, run towards
             // If we are in shoot range, maybe stop to shoot?
             // Only stop if we CAN shoot (i.e. has performed first melee)
             if (!forceCloseIn && seesPlayer && dist >= _config.shootRangeMin && dist <= _config.shootRangeMin + 2f && Time.time < _nextShootAttackTime)
             {
                 // Maybe wait for cooldown?
                 _motor.Stop();
             }
             else
             {
                 _motor.Move(Mathf.Sign(dx) * _config.agroRunSpeed);
             }
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
        _hasPerformedFirstMelee = true; // Mark as done
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
        // Spawn is now handled by Animation Event (AnimationEvent_SpawnProjectile)
        _nextShootAttackTime = Time.time + _config.shootAttackCooldown;
    }
    
    // Called by Animation Event
    public void AnimationEvent_SpawnProjectile()
    {
        if (_state != State.AttackShoot) return;
        SpawnProjectile();
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
        
        // Loop logic:
        if (_config != null && _config.loopPath)
        {
            _pathIndex = (_pathIndex + 1) % _path.Count;
            return;
        }

        // Ping-pong logic:
        int next = _pathIndex + _pathDir;
        if (next >= _path.Count || next < 0)
        {
            _pathDir *= -1; // Reverse direction
            next = _pathIndex + _pathDir; 
            
            // Safety clamp if count is small (e.g. 2 points: 0->1. At 1, dir=+1, next=2(out). dir=-1, next=0. Correct.)
            next = Mathf.Clamp(next, 0, _path.Count - 1);
        }
        _pathIndex = next;
    }
    
    public void SetPatrolPath(EnemyPatrolPath path)
    {
        _patrolPath = path;
        if (_patrolPath != null && _patrolPath.Count > 0)
        {
            _pathIndex = FindNearestWaypointIndex(transform.position);
        }
    }

    void OnDrawGizmos()
    {
        if (_config == null) return;

        // Draw Close Range (Melee)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _config.closeRangeThreshold);

        // Draw Shoot Range (Min distance)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _config.shootRangeMin);
    }
}
