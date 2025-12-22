using UnityEngine;
using Zenject;

public class WormBrain : MonoBehaviour
{
    public enum State
    {
        Patrol,
        Trigger,  // Windup logic state (waiting for anim)
        Spinning, // Active attack state
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
    
    // Logic
    bool _lastHitWasWall;
    bool _isBoosted;

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
        
        // Start in Patrol
        EnterPatrol();
        
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
            
            // Check if blocked (Wall or Ledge)
            if (_motor.IsWallAhead(_patrolDir) || _motor.IsLedgeAhead(_patrolDir))
            {
                _path = null;
                _patrolDir *= -1;
            }
        }
        else
        {
            // Wall Patrol
            if (_motor.IsLedgeAhead(_patrolDir) || _motor.IsWallAhead(_patrolDir))
            {
                 _patrolDir *= -1;
            }
        }
        
        _motor.Move(_config.patrolSpeed, _config.patrolAcceleration, _patrolDir);
    }

    void TickTrigger()
    {
        // Waiting for Animator to finish "Trigger" anim and enter "Spinning" state
        _motor.SetFrozen(true);
        
        // Safety timeout in case animator gets stuck (e.g. no transition)
        if (_stateTimer > 3.0f)
        {
            Debug.LogWarning("[Worm] Trigger state timed out. Forcing Spin.");
            EnterSpinningLogic(); 
            return;
        }

        if (_anim && _anim.IsInSpinning())
        {
            EnterSpinningLogic();
        }
    }

    void TickSpinning()
    {
        _motor.SetFrozen(false);
        
        // Check for player jump over if not already boosted
        if (!_isBoosted && _motor.CheckPlayerAbove(_config.jumpOverRayHeight))
        {
            _isBoosted = true;
            // Optionally could add a debug log here
            // Debug.Log("[Worm] Player jumped over! Boosting speed.");
        }

        float currentSpeed = _config.chargeSpeed * (_isBoosted ? _config.jumpOverSpeedMultiplier : 1f);

        // Movement
        _motor.Move(currentSpeed, _config.chargeAcceleration, (int)Mathf.Sign(_spinDirection.x));

        // Check Hit (Wall or Player)
        // Add small delay to avoid hitting self/floor immediately if weird collision
        if (_stateTimer > 0.05f) 
        {
            if (_motor.CheckWallHit(out Vector2 wallNormal))
            {
                Debug.Log($"[Worm] Hit Wall! Normal: {wallNormal}");
                _lastHitWasWall = true;
                EnterStun();
                return;
            }

            if (CheckPlayerHit(out Vector2 away))
            {
                Debug.Log($"[Worm] Hit Player! Away: {away}");
                _lastHitWasWall = false;
                EnterStun();
                return;
            }
        }
    }

    void TickStun(bool seesPlayer)
    {
        _motor.ApplyDrag(_config.stunDrag);
        
        bool animFinished = _anim == null || _anim.IsStunFinished();
        bool timeout = _stateTimer > 5.0f;

        if (animFinished || timeout)
        {
            _motor.ResetDrag();
            
            // Decision: Attack again or Patrol?
            if (_forgetTimer > 0f)
            {
                // Attack Again
                if (_lastHitWasWall)
                {
                    // Bounce/Roll back: invert direction
                    _spinDirection = new Vector2(-Mathf.Sign(_spinDirection.x), 0f);
                    EnterTrigger(true); 
                }
                else
                {
                    // Hit player: chase player
                    EnterTrigger(false); 
                }
            }
            else
            {
                // Back to patrol
                EnterPatrol();
            }
        }
    }

    // --- Transitions ---

    void EnterPatrol()
    {
        Debug.Log("[Worm] Enter Patrol");
        _state = State.Patrol;
        _stateTimer = 0;
        
        _anim?.ResetAllTriggers();
        _anim?.TriggerPatrol();
        
        _motor.SetFrozen(false);
        
        // Resume path?
        if (_patrolPath != null) _path = _patrolPath;
    }

    void EnterTrigger(bool preserveDirection = false)
    {
        Debug.Log("[Worm] Enter Trigger (Windup)");
        _state = State.Trigger;
        _stateTimer = 0;
        
        // 1. Setup direction
        if (!preserveDirection)
        {
            if (_target)
            {
                float dx = _target.position.x - transform.position.x;
                if (Mathf.Abs(dx) > 0.1f)
                    _spinDirection = new Vector2(Mathf.Sign(dx), 0);
                else
                    _spinDirection = new Vector2(transform.localScale.x, 0);
            }
            else
            {
                _spinDirection = new Vector2(transform.localScale.x, 0);
            }
        }
        _motor.Face((int)_spinDirection.x);

        // 2. Setup Animator Triggers
        // We set BOTH TriggerAttack (to enter Trigger state) 
        // AND SpinningTrigger (to automatically exit Trigger -> Spinning after ExitTime)
        _anim?.ResetAllTriggers();
        _anim?.TriggerAttack();
        _anim?.TriggerSpinning();
        
        // 3. Freeze until animation finishes
        _motor.SetFrozen(true);
    }
    
    // Called ONLY when we detect Animator has actually entered Spinning state
    void EnterSpinningLogic()
    {
        Debug.Log("[Worm] Enter Spinning (Logic)");
        _state = State.Spinning;
        _stateTimer = 0;
        _isBoosted = false;
        _motor.SetFrozen(false);
        // Note: No need to set triggers here, we are already in the state
    }

    void EnterStun()
    {
        Debug.Log("[Worm] Enter Stun");
        _state = State.Stun;
        _stateTimer = 0;
        
        _anim?.ResetAllTriggers();
        _anim?.TriggerStun();
        
        _motor.SetFrozen(false);
    }

    void EnterDead()
    {
        Debug.Log("[Worm] Dead");
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
    
    bool CheckPlayerHit(out Vector2 away)
    {
        away = Vector2.zero;
        if (_config.playerLayer.value == 0) return false;
        
        Collider2D hit = Physics2D.OverlapCircle(transform.position, 0.8f, _config.playerLayer);
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
        
        if (current < _lastHp)
        {
            _forgetTimer = _config.aggroForgetSeconds;
            if (_state == State.Patrol)
            {
                 _patrolDir *= -1;
                 _motor.Face(_patrolDir);
                 EnterTrigger();
            }
        }
        _lastHp = current;
    }
    
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
