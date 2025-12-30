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
    
    // Internal flags for melee sequence
    bool _attack1Triggered;
    bool _attack2Triggered;

    int _lastHp;
    
    float _zoneMinX;
    float _zoneMaxX;
    bool _zoneReady;

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
    
    void OnEnable()
    {
        if (_health)
        {
            _lastHp = _health.CurrentHP;
            _health.OnHealthChanged += OnHealthChanged;
        }
    }

    void OnDisable()
    {
        if (_health) _health.OnHealthChanged -= OnHealthChanged;
    }

    void Start()
    {
        _state = State.Patrol;
        if (_path != null && _path.Count > 0)
        {
            _pathIndex = FindNearestWaypointIndex(transform.position);
        }
        
        RecalcZoneBoundsFromPath();
        
        // Try find player
         var pf = FindObjectOfType<PlayerFacade>();
         if (pf != null) _player = pf.transform;
    }
    
    void RecalcZoneBoundsFromPath()
    {
        _zoneReady = false;
        if (_path == null || _path.Count == 0) return;

        float minX = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;

        for (int i = 0; i < _path.Count; i++)
        {
            float x = _path.GetPoint(i).x;
            if (x < minX) minX = x;
            if (x > maxX) maxX = x;
        }

        const float pad = 0.05f;
        _zoneMinX = minX - pad;
        _zoneMaxX = maxX + pad;
        _zoneReady = true;
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
        
        // 1. Calculate distance purely on X if it's a 2D platformer on flat ground
        // We use full distance normally, but if we are "stuck" we might need X only check.
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
        
        // Anti-stuck: if dx is tiny but dist is large (meaning Y diff), force advance if close enough in X?
        // OR simply ignore Y for arrival check if it's a flat platformer.
        // Let's rely on X distance for arrival if configured, or just be more lenient.
        if (Mathf.Abs(dx) < 0.1f) 
        {
             // Close enough in X. Treating as arrived to prevent oscillation.
             AdvancePathIndex();
             _motor.Stop();
             return;
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

        Vector3 rawTargetPos = seesPlayer ? _player.position : (Vector3)_lastSeenPos;

        float distToPlayer = Vector2.Distance(transform.position, rawTargetPos);
        float dxToPlayer = rawTargetPos.x - transform.position.x;

        Vector3 moveTargetPos = rawTargetPos;
        if (_zoneReady)
            moveTargetPos.x = Mathf.Clamp(rawTargetPos.x, _zoneMinX, _zoneMaxX);

        float distToMoveTarget = Vector2.Distance(transform.position, moveTargetPos);
        float dxToMoveTarget = moveTargetPos.x - transform.position.x;

        if (Mathf.Abs(dxToPlayer) > 0.1f)
            _motor.Face(dxToPlayer > 0 ? 1 : -1);

        if (_zoneReady)
        {
            if (transform.position.x < _zoneMinX)
            {
                _motor.Move(_config.agroRunSpeed);
                return;
            }

            if (transform.position.x > _zoneMaxX)
            {
                _motor.Move(-_config.agroRunSpeed);
                return;
            }
        }

        if (distToPlayer <= _config.closeRangeThreshold)
        {
            if (Time.time >= _nextMeleeAttackTime)
            {
                StartMeleeAttack();
                return;
            }
        }
        else if (seesPlayer && distToPlayer >= _config.shootRangeMin)
        {
            if (Time.time >= _nextShootAttackTime)
            {
                StartShootAttack();
                return;
            }
        }

        if (distToMoveTarget > _config.closeRangeThreshold * 0.8f)
        {
            if (seesPlayer && distToPlayer >= _config.shootRangeMin &&
                distToPlayer <= _config.shootRangeMin + 2f && Time.time < _nextShootAttackTime)
            {
                _motor.Stop();
            }
            else
            {
                if (Mathf.Abs(dxToMoveTarget) > 0.05f)
                    _motor.Move(Mathf.Sign(dxToMoveTarget) * _config.agroRunSpeed);
                else
                    _motor.Stop();
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
        _nextMeleeAttackTime = Time.time + _config.meleeAttackCooldown;
    }

    void TickAttackMelee()
    {
        // Force sequencer: Attack1 -> Attack2
        
        // If we are currently playing Attack1, we MUST ensure Attack1 bool is off so it doesn't loop,
        // and set Attack2 bool ON to queue the transition.
        if (_anim.IsInAttack1())
        {
             // We want to transition to Attack2. 
             // The transition condition is "Attack2 == true".
             // The exit transition from Attack1 to Agro is "Attack1 == false".
             // Since we want to go to Attack2, we must ensure Attack2 is true.
             // Usually, turning off Attack1 is fine as long as Attack2 is set, priority depends on Animator.
             // But to be safe, let's keep Attack1 true until we are sure? 
             // No, "Attack1 = false" triggers exit. So we should NOT set Attack1 false if we want to chain?
             // Actually, usually "Has Exit Time" or specific conditions handle this.
             // User says: "From Attack1 transition bool Attack1=true -> Attack1State" (wait, entering).
             // "From Attack1 transition bool Attack2=true -> Attack2State".
             // "From Attack1 transition bool Attack1=false -> AgroMovement".
             
             // So if we want to go 1 -> 2:
             // We must Set Attack2 = true.
             // We must NOT Set Attack1 = false immediately if that causes early exit before Attack2 transition is picked up?
             // Unity picks first valid transition.
             
             if (!_attack2Triggered)
             {
                 _anim.SetAttack2(true);
                 _attack2Triggered = true;
                 
                 // We can turn off Attack1 now, assuming Attack2 transition will take precedence or 
                 // Attack1->Agro has a condition that we can avoid?
                 // User said: "Attack1 transition bool Attack1=false -> AgroMovement".
                 // So if we set Attack1=false, it might go to Agro.
                 // We should keep Attack1=true UNTIL we are in Attack2? 
                 // But then it might loop Attack1 if it's set to loop?
                 // Assuming Attack1 is not looping.
                 
                 // Let's keep Attack1 TRUE until we see we are in Attack2 state.
             }
        }
        
        if (_anim.IsInAttack2())
        {
            // Now we are safely in Attack2.
            // Turn off triggers/bools.
            _anim.SetAttack1(false);
            _anim.SetAttack2(false); 
        }
        else if (_attack2Triggered && !_anim.IsInAttack1()) 
        {
            // We triggered attack 2, and we are NOT in attack 1 anymore.
            // Maybe we are transitioning? Or maybe we finished?
            // If we are back in Agro, then the combo finished.
        }

        // Check for exit
        // If we finished Attack2 and returned to Agro
        if (_anim.IsInAgroMovement() && _attack2Triggered) 
        {
             _state = State.Agro;
             // Reset flags
             _anim.SetAttack1(false);
             _anim.SetAttack2(false);
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
            // Request: shoot strictly straight, no aiming.
            
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
        if (next >= _path.Count)
        {
            _pathDir = -1;
            next = Mathf.Max(0, _path.Count - 2); 
        }
        else if (next < 0)
        {
            _pathDir = 1;
            next = Mathf.Min(1, _path.Count - 1);
        }

        // Final safety check
        next = Mathf.Clamp(next, 0, _path.Count - 1);

        _pathIndex = next;
    }
    
    public void SetPatrolPath(EnemyPatrolPath path)
    {
        _patrolPath = path;
        _path = _patrolPath;          
        _pathDir = 1;                

        if (_path != null && _path.Count > 0)
            _pathIndex = FindNearestWaypointIndex(transform.position);
        
        RecalcZoneBoundsFromPath();
    }

    void OnHealthChanged()
    {
        if (_health == null) return;
        int current = _health.CurrentHP;
        
        if (current < _lastHp)
        {
            // If we took damage and are not already in combat (agro/attack)
            // Or even if we are, maybe we should turn to face the damage source?
            // Worm logic: if in patrol, turn and enter trigger.
            
            if (_state == State.Patrol)
            {
                // Try to find source
                if (_health.LastHit != null && _health.LastHit.source != null)
                {
                    Transform src = _health.LastHit.source.transform;
                    float dx = src.position.x - transform.position.x;
                    if (Mathf.Abs(dx) > 0.1f)
                    {
                        _motor.Face(dx > 0 ? 1 : -1);
                    }
                    
                    // Also remember position as last seen?
                    _lastSeenPos = src.position;
                    // Trigger Agro
                    EnterTriggerAgro();
                }
            }
        }
        _lastHp = current;
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
