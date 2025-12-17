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
    float _stunTimer;

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
        
        // Ledge Check
        if (_motor.IsLedgeAhead(_patrolDir) || _motor.IsWallAhead(_patrolDir))
        {
             // If we hit a wall or ledge in patrol
             // If we have a path, we usually stick to it, but simple fallback:
             _patrolDir *= -1;
             
             // If path exists, maybe we are stuck?
             // Simple fallback: just reverse
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

            // Player Hit (Hitbox check)
            // Ideally we use a Trigger collider on the enemy to detect player
            // But here we can use OverlapBox or similar if we don't have the event
            // Let's assume we rely on collisions or a simple check
            if (CheckPlayerHit(out Vector2 away))
            {
                Bounce(away);
                return;
            }
        }
    }

    void TickStun(bool seesPlayer)
    {
        _motor.ApplyDrag(_config.stunDrag);
        
        // Wait for Animator to enter Stun state and finish clip
        // Use a small delay to avoid frame 0 exit before transition happens
        if (_stateTimer < 0.2f) return;

        bool animFinished = _anim != null && _anim.IsStunFinished();
        
        // Safety timeout in case animation fails or is missing
        bool timeout = _stateTimer > 5.0f;

        if (animFinished || timeout)
        {
            _motor.ResetDrag();
            
            // Re-eval aggression logic:
            // "If timer not finished -> Spin other way"
            if (_forgetTimer > 0f)
            {
                // Simple Ping-Pong: reverse direction from current facing
                int currentSign = (int)Mathf.Sign(transform.localScale.x);
                int nextSign = -currentSign;
                
                // Set spin direction immediately
                _spinDirection = new Vector2(nextSign, 0f);
                
                // Enter Trigger (Windup) to telegraph next roll
                // Note: EnterTrigger usually calculates direction from Target.
                // We override this by explicitly setting facing after enter.
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
        _anim?.TriggerPatrol();
        _motor.SetFrozen(false);
    }

    void EnterTrigger()
    {
        _state = State.Trigger;
        _stateTimer = 0;
        _anim?.TriggerAttack();
        
        // Face target if known
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
        _anim?.TriggerSpinning();
        _motor.SetFrozen(false);
    }

    void EnterStun()
    {
        _state = State.Stun;
        _stateTimer = 0;
        _anim?.TriggerStun();
        _motor.SetFrozen(false);
    }

    void EnterDead()
    {
        var prevState = _state;
        _state = State.Dead;
        _motor.SetFrozen(true);
        _motor.StopAllCoroutines(); // just in case
        
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
        // Simple arc away from normal
        Vector2 bounceDir = (impactNormal + Vector2.up).normalized;
        // Or strictly calculated like in old script
        
        // Calculate velocity for Arc
        float g = Mathf.Abs(Physics2D.gravity.y);
        float h = _config.bounceArcHeight;
        float dist = _config.bounceArcDistance;
        
        float vy = Mathf.Sqrt(2 * g * h);
        float t = 2 * vy / g;
        float vx = dist / t;
        
        float dirX = -Mathf.Sign(_spinDirection.x); // Bounce back
        
        _motor.SetVelocity(new Vector2(dirX * vx, vy));
    }
    
    bool CheckPlayerHit(out Vector2 away)
    {
        away = Vector2.zero;
        // Simple overlap check for player
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
                 // Turn around
                 _patrolDir *= -1;
                 _motor.Face(_patrolDir);
                 
                 // Trigger aggro
                 EnterTrigger();
                 
                 // Update forget timer so we don't immediately lose interest
                 _forgetTimer = _config.aggroForgetSeconds;
                 
                 // Try to set direction to where damage came from if possible, 
                 // but "turn around" is usually sufficient if hit from back.
            }
        }
        _lastHp = current;
    }
    
    // --- Patrol Utils ---
    
    void AdvancePathIndex()
    {
        if (_path == null || _path.Count <= 1) return;
        _pathIndex++;
        if (_pathIndex >= _path.Count) _pathIndex = 0; // Loop by default for now
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
