using UnityEngine;
using Zenject;

public class WormBrain : MonoBehaviour
{
    public enum State
    {
        Patrol,
        Trigger,  // Windup
        Spinning, // Attack
        Stun,
        Dead
    }

    [Header("Refs")]
    [SerializeField] WormMotor2D _motor;
    [SerializeField] WormAnimatorAdapter _anim;
    [SerializeField] VisionCone2D _vision;
    [SerializeField] Health _health;
    [SerializeField] EnemyPatrolPath _patrolPath;

    [Inject]
    public void Construct(WormConfigSO config, IHealth health)
    {
        _config = config;
        _iHealth = health;
    }

    WormConfigSO _config;
    IHealth _iHealth;
    State _state;
    
    // Runtime
    Transform _target;
    Vector2 _spinDirection;
    float _stateTimer;
    float _forgetTimer;
    
    // Patrol Runtime
    EnemyPatrolPath _path;
    int _pathIndex;
    int _patrolDir = 1;

    // Health watch
    int _lastHp;
    
    // Spin/Bounce
    bool _isBouncing;

    public void SetPatrolPath(EnemyPatrolPath path)
    {
        _patrolPath = path;
        _path = _patrolPath;
        if (_path != null && _path.Count > 0)
        {
            _pathIndex = FindNearestWaypointIndex(transform.position);
        }
    }

    void Awake()
    {
        if (!_motor) _motor = GetComponent<WormMotor2D>();
        if (!_anim) _anim = GetComponentInChildren<WormAnimatorAdapter>(true);
        if (!_vision) _vision = GetComponentInChildren<VisionCone2D>(true);
        if (!_health) _health = GetComponent<Health>();
        
        if (_iHealth == null && _health) _iHealth = _health;
    }

    void OnEnable()
    {
        if (_health)
        {
            _lastHp = _health.CurrentHP;
            _health.OnHealthChanged += OnHealthChanged;
        }
        
        _state = State.Patrol;
        _anim?.ResetAllTriggers();
        _anim?.TriggerPatrol();
        
        if (_patrolPath == null)
            _patrolPath = FindNearestPatrolPath();
        _path = _patrolPath;
        if (_path != null && _path.Count > 0)
            _pathIndex = FindNearestWaypointIndex(transform.position);
    }

    void OnDisable()
    {
        if (_health) _health.OnHealthChanged -= OnHealthChanged;
    }

    void Update()
    {
        if (_config == null || _iHealth == null) return;

        if (!_iHealth.IsAlive && _state != State.Dead)
        {
            EnterDead();
            return;
        }

        if (_state == State.Dead) return;

        _stateTimer += Time.deltaTime;

        // Vision Check
        Transform t = null;
        bool seesPlayer = _vision && _vision.TryGetClosestTarget(out t);
        if (seesPlayer)
        {
            _target = t;
            _forgetTimer = _config.aggroForgetSeconds;
        }
        else
        {
            _forgetTimer -= Time.deltaTime;
        }

        // Logic FSM
        switch (_state)
        {
            case State.Patrol:
                TickPatrol(seesPlayer);
                break;
            case State.Trigger:
                TickTrigger();
                break;
            case State.Spinning:
                TickSpinning();
                break;
            case State.Stun:
                TickStun(seesPlayer);
                break;
        }
    }

    void TickPatrol(bool seesPlayer)
    {
        // 1. Check Aggro
        if (seesPlayer)
        {
            EnterTrigger();
            return;
        }

        // 2. Move Patrol
        if (_path != null && _path.Count > 0)
        {
            Vector2 targetPt = _path.GetPoint(_pathIndex);
            float dx = targetPt.x - transform.position.x;
            
            // Reached waypoint?
            if (Mathf.Abs(dx) < 0.2f) 
            {
                AdvancePathIndex();
                targetPt = _path.GetPoint(_pathIndex);
                dx = targetPt.x - transform.position.x;
            }

            _patrolDir = dx >= 0 ? 1 : -1;
        }
        
        // Ledge/Wall Check ONLY if we don't have a valid path (or path logic failed)
        // If we have a path, we trust the waypoints.
        bool hasPath = _path != null && _path.Count > 0;
        
        if (!hasPath)
        {
            if (_motor.IsLedgeAhead(_patrolDir) || _motor.IsWallAhead(_patrolDir))
            {
                 _patrolDir *= -1;
            }
        }
        
        _motor.Move(_config.patrolSpeed, _config.patrolAcceleration, _patrolDir);
    }

    void TickTrigger()
    {
        // Wait for windup time
        _motor.SetFrozen(true);
        if (_stateTimer >= _config.windupTime)
        {
            EnterSpinning();
        }
    }

    void TickSpinning()
    {
        _motor.SetFrozen(false);
        
        if (_isBouncing)
        {
            // Arc movement is handled by physics (velocity set at start of bounce)
            // Wait for ground
            if (_motor.Rigidbody.linearVelocity.y <= 0 && Physics2D.Raycast(transform.position, Vector2.down, 0.1f, _config.solidLayers)) // grounded check
            {
                EnterStun();
            }
            return;
        }

        // Charge
        _motor.Move(_config.chargeSpeed, _config.chargeAcceleration, (int)Mathf.Sign(_spinDirection.x));

        // Check Hit (Wall or Player)
        if (_stateTimer > _config.spinMinDuration)
        {
            // Wall Hit
            if (_motor.CheckWallHit(out Vector2 wallNormal))
            {
                Bounce(wallNormal);
                return;
            }

            // Player Hit
            if (CheckPlayerHit(out Vector2 away))
            {
                Bounce(away);
                return;
            }
        }
        
        // Note: We DO NOT exit Spinning by timer. Only by impact.
    }

    void TickStun(bool seesPlayer)
    {
        _motor.ApplyDrag(_config.stunDrag);
        
        // Always wait for minimum config duration
        if (_stateTimer < _config.stunDuration) return;

        // Optionally wait for animation
        bool animFinished = _anim == null || _anim.IsStunFinished();
        
        // Safety timeout
        bool timeout = _stateTimer > 5.0f;

        if (animFinished || timeout)
        {
            _motor.ResetDrag();
            
            // Re-eval aggression logic
            if (_forgetTimer > 0f)
            {
                // Timer still active -> Attack again!
                
                // If we see player (or saw recently and hit player), try to face them.
                // If we just hit a wall and reversed, we might not see player, but we should continue patrolling/attacking.
                
                // Simple Logic from request: "Change direction to opposite and roll until hit wall/player"
                int currentSign = (int)Mathf.Sign(transform.localScale.x);
                int nextSign = -currentSign;
                
                // If we actually see player right now, prioritize player direction
                if (seesPlayer && _target != null)
                {
                     float dx = _target.position.x - transform.position.x;
                     if (Mathf.Abs(dx) > 0.1f) nextSign = (int)Mathf.Sign(dx);
                }
                
                _spinDirection = new Vector2(nextSign, 0f);
                
                // Enter Trigger (Windup)
                EnterTrigger();
                
                _motor.Face(nextSign);
            }
            else
            {
                // Timer expired -> Patrol
                EnterPatrol();
            }
        }
    }

    // --- Transitions ---

    void EnterPatrol()
    {
        _state = State.Patrol;
        _stateTimer = 0;
        _anim?.ResetAllTriggers();
        _anim?.TriggerPatrol();
        _motor.SetFrozen(false);
    }

    void EnterTrigger()
    {
        _state = State.Trigger;
        _stateTimer = 0;
        _anim?.ResetAllTriggers();
        _anim?.TriggerAttack();
        
        // Logic for initial facing usually happens before calling EnterTrigger or inside tick
        // But here we set facing if target is known, otherwise keep current.
        if (_target)
        {
            float dx = _target.position.x - transform.position.x;
            if (Mathf.Abs(dx) > 0.1f)
            {
                _spinDirection = new Vector2(Mathf.Sign(dx), 0);
                _motor.Face((int)_spinDirection.x);
            }
            else
            {
                 _spinDirection = new Vector2(transform.localScale.x, 0);
            }
        }
        else
        {
            _spinDirection = new Vector2(transform.localScale.x, 0);
        }
    }

    void EnterSpinning()
    {
        _state = State.Spinning;
        _stateTimer = 0;
        _isBouncing = false;
        _anim?.ResetAllTriggers();
        _anim?.TriggerSpinning();
        _motor.SetFrozen(false);
    }

    void EnterStun()
    {
        _state = State.Stun;
        _stateTimer = 0;
        _anim?.ResetAllTriggers();
        _anim?.TriggerStun();
        _motor.SetFrozen(false);
    }

    void EnterDead()
    {
        var prevState = _state;
        _state = State.Dead;
        _motor.SetFrozen(true);
        _motor.StopAllCoroutines(); 
        
        if (prevState == State.Spinning)
            _anim?.TriggerSpinningDeath();
        else
            _anim?.TriggerPatrolDeath();
            
        enabled = false;
    }

    // --- Helpers ---

    void Bounce(Vector2 impactNormal)
    {
        _isBouncing = true;
        
        // Calculate bounce velocity
        float g = Mathf.Abs(Physics2D.gravity.y);
        float h = _config.bounceArcHeight;
        float dist = _config.bounceArcDistance;
        
        float vy = Mathf.Sqrt(2 * g * h);
        float t = 2 * vy / g;
        float vx = dist / t;
        
        float dirX = -Mathf.Sign(_spinDirection.x); // Bounce back against spin direction
        
        _motor.SetVelocity(new Vector2(dirX * vx, vy));
    }
    
    bool CheckPlayerHit(out Vector2 away)
    {
        away = Vector2.zero;
        if (_config.playerLayer.value == 0) return false;
        
        Collider2D hit = Physics2D.OverlapCircle(transform.position, 1.0f, _config.playerLayer);
        if (hit)
        {
            away = (transform.position - hit.transform.position).normalized;
            return true;
        }
        return false;
    }

    void OnHealthChanged()
    {
        if (_health == null) return;
        int current = _health.CurrentHP;
        
        // If hit and in Patrol and didn't see player -> turn and aggro
        if (current < _lastHp)
        {
            if (_state == State.Patrol && _forgetTimer <= 0)
            {
                 _patrolDir *= -1;
                 _motor.Face(_patrolDir);
                 
                 EnterTrigger();
                 
                 _forgetTimer = _config.aggroForgetSeconds;
            }
        }
        _lastHp = current;
    }
    
    // --- Patrol Utils ---
    
    void AdvancePathIndex()
    {
        if (_path == null || _path.Count <= 1) return;
        _pathIndex++;
        if (_pathIndex >= _path.Count) _pathIndex = 0;
    }

    int FindNearestWaypointIndex(Vector2 pos)
    {
        if (_path == null || _path.Count == 0) return 0;
        int best = 0;
        float minDst = float.MaxValue;
        for (int i=0; i<_path.Count; i++)
        {
            float d = Vector2.Distance(pos, _path.GetPoint(i));
            if (d < minDst) { minDst = d; best = i; }
        }
        return best;
    }
    
    EnemyPatrolPath FindNearestPatrolPath()
    {
        var all = FindObjectsOfType<EnemyPatrolPath>(true);
        if (all == null || all.Length == 0) return null;
        
        EnemyPatrolPath best = null;
        float minDist = float.MaxValue;
        
        foreach(var p in all)
        {
            float d = Vector2.Distance(transform.position, p.transform.position);
            if (d < minDist) { minDist = d; best = p; }
        }
        return best;
    }
}
