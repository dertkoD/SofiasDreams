using UnityEngine;
using Zenject;

public class WormBrain : BaseEnemyBrain
{
    [Header("Refs")]
    [SerializeField] WormMotor2D _motor;
    [SerializeField] WormAnimatorAdapter _anim;
    [SerializeField] VisionCone2D _vision;
    [SerializeField] Health _health;
    [SerializeField] EnemyPatrolPath _patrolPath;
    [SerializeField] EnemyContactDamage _contactDamage;

    public WormMotor2D Motor => _motor;
    public WormAnimatorAdapter Anim => _anim;
    public VisionCone2D Vision => _vision;
    public Health Health => _health;
    public EnemyPatrolPath PatrolPath { get => _patrolPath; set => _patrolPath = value; }
    public EnemyContactDamage ContactDamage => _contactDamage;

    public WormConfigSO Config { get; private set; }
    public IHealth IHealth { get; private set; }

    // States
    public WormPatrolState PatrolState { get; private set; }
    public WormTriggerState TriggerState { get; private set; }
    public WormSpinState SpinState { get; private set; }
    public WormStunState StunState { get; private set; }
    public WormDeadState DeadState { get; private set; }

    // Runtime Data
    public Transform Target { get; set; }
    public Vector2 SpinDirection { get; set; }
    public bool LastHitWasWall { get; set; }
    public float StateTimer { get; set; }
    public float ForgetTimer { get; set; }

    // Patrol Runtime
    [HideInInspector] public EnemyPatrolPath CurrentPath;
    [HideInInspector] public int PathIndex;
    [HideInInspector] public int PatrolDir = 1;

    int _lastHp;

    [Inject]
    public void Construct(WormConfigSO config, IHealth health)
    {
        Config = config;
        IHealth = health;
    }

    void Awake()
    {
        if (!_motor) _motor = GetComponent<WormMotor2D>();
        if (!_anim) _anim = GetComponentInChildren<WormAnimatorAdapter>(true);
        if (!_vision) _vision = GetComponentInChildren<VisionCone2D>(true);
        if (!_health) _health = GetComponent<Health>();
        if (!_contactDamage) _contactDamage = GetComponent<EnemyContactDamage>();
        if (IHealth == null && _health) IHealth = _health;

        PatrolState = new WormPatrolState(this);
        TriggerState = new WormTriggerState(this);
        SpinState = new WormSpinState(this);
        StunState = new WormStunState(this);
        DeadState = new WormDeadState(this);
    }

    void OnEnable()
    {
        if (Health)
        {
            _lastHp = Health.CurrentHP;
            Health.OnHealthChanged += OnHealthChanged;
        }

        if (ContactDamage) ContactDamage.OnPlayerContact += OnPlayerContact;

        if (PatrolPath == null)
            PatrolPath = FindNearestPatrolPath();
        CurrentPath = PatrolPath;
        if (CurrentPath != null && CurrentPath.Count > 0)
            PathIndex = FindNearestWaypointIndex(transform.position);

        ChangeState(PatrolState);
    }

    void OnDisable()
    {
        if (Health) Health.OnHealthChanged -= OnHealthChanged;
        if (ContactDamage) ContactDamage.OnPlayerContact -= OnPlayerContact;
    }

    protected override void Update()
    {
        if (Config == null || IHealth == null) return;

        if (!IHealth.IsAlive && CurrentState != DeadState)
        {
            ChangeState(DeadState);
            return;
        }

        StateTimer += Time.deltaTime;

        // Global Vision Logic
        Transform t = null;
        bool seesPlayer = Vision && Vision.TryGetClosestTarget(out t);
        if (seesPlayer)
        {
            Target = t;
            ForgetTimer = Config.aggroForgetSeconds;
        }
        else
        {
            ForgetTimer -= Time.deltaTime;
        }

        // Run State Tick
        base.Update();
    }

    void OnPlayerContact()
    {
        if (CurrentState == PatrolState)
        {
            if (CurrentPath != null) CurrentPath = null;
            PatrolDir *= -1;
            Motor.Face(PatrolDir);
        }
    }

    void OnHealthChanged()
    {
        if (Health == null) return;
        int current = Health.CurrentHP;

        if (current < _lastHp)
        {
            ForgetTimer = Config.aggroForgetSeconds;
            if (CurrentState == PatrolState)
            {
                PatrolDir *= -1;
                Motor.Face(PatrolDir);
                ChangeState(TriggerState);
            }
        }
        _lastHp = current;
    }

    // --- Helpers ---

    public void AdvancePathIndex()
    {
        if (CurrentPath == null || CurrentPath.Count <= 1) return;
        PathIndex++;
        if (PathIndex >= CurrentPath.Count) PathIndex = 0;
    }

    public int FindNearestWaypointIndex(Vector2 pos)
    {
        if (CurrentPath == null || CurrentPath.Count == 0) return 0;
        int best = 0;
        float minDst = float.MaxValue;
        for (int i = 0; i < CurrentPath.Count; i++)
        {
            float d = Vector2.Distance(pos, CurrentPath.GetPoint(i));
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

        foreach (var p in all)
        {
            float d = Vector2.Distance(transform.position, p.transform.position);
            if (d < minDist) { minDist = d; best = p; }
        }
        return best;
    }

    public bool CheckPlayerHit(out Vector2 away)
    {
        away = Vector2.zero;
        if (Config.playerLayer.value == 0) return false;

        Collider2D hit = Physics2D.OverlapCircle(transform.position, 0.8f, Config.playerLayer);
        if (hit)
        {
            away = (transform.position - hit.transform.position).normalized;
            return true;
        }
        return false;
    }
}

// --- States ---

public class WormPatrolState : IEnemyState
{
    WormBrain _brain;
    public WormPatrolState(WormBrain brain) { _brain = brain; }

    public void Enter()
    {
        Debug.Log("[Worm] Enter Patrol");
        _brain.StateTimer = 0;
        _brain.Anim?.ResetAllTriggers();
        _brain.Anim?.TriggerPatrol();
        _brain.Motor.SetFrozen(false);
        if (_brain.PatrolPath != null) _brain.CurrentPath = _brain.PatrolPath;
    }

    public void Tick()
    {
        bool seesPlayer = _brain.ForgetTimer > 0 && _brain.Target != null; 
        // Note: The original used 'seesPlayer' bool passed from Update, which was strictly Vision check.
        // But logic also checked Aggro: "if (seesPlayer) EnterTrigger".
        // Here we use ForgetTimer which is updated by vision.
        // Original: "if (seesPlayer) EnterTrigger". seesPlayer was local var from vision check.
        
        // Wait, original:
        // bool seesPlayer = _vision.TryGetClosestTarget...
        // TickPatrol(seesPlayer) -> if (seesPlayer) EnterTrigger.
        // It didn't use ForgetTimer for entering trigger from patrol, only direct sight?
        // Let's check original... 
        // Yes: "if (seesPlayer) EnterTrigger();"
        // And ForgetTimer was updated but not used for STARTING aggro in patrol?
        
        // Re-reading original Update:
        // bool seesPlayer = ...
        // if (seesPlayer) { ... _forgetTimer = ... } else { ... }
        // case Patrol: TickPatrol(seesPlayer);
        // void TickPatrol(bool seesPlayer) { if (seesPlayer) EnterTrigger(); ... }
        
        // So strict vision required to start aggro.
        
        // However, OnHealthChanged also calls EnterTrigger.
        
        // I will access vision check directly if possible or rely on Brain's update.
        // Brain updates Target and ForgetTimer.
        // But Brain doesn't expose "IsSeesPlayerThisFrame".
        // I can check Vision again or check ForgetTimer == Max?
        // Or I can change Brain to expose "SeesPlayer".
        
        if (_brain.Vision && _brain.Vision.TryGetClosestTarget(out var t))
        {
            _brain.ChangeState(_brain.TriggerState);
            return;
        }

        // Patrol Move
        if (_brain.CurrentPath != null && _brain.CurrentPath.Count > 0)
        {
            Vector2 targetPt = _brain.CurrentPath.GetPoint(_brain.PathIndex);
            float dx = targetPt.x - _brain.transform.position.x;

            if (Mathf.Abs(dx) < 0.2f)
            {
                _brain.AdvancePathIndex();
                targetPt = _brain.CurrentPath.GetPoint(_brain.PathIndex);
                dx = targetPt.x - _brain.transform.position.x;
            }

            _brain.PatrolDir = dx >= 0 ? 1 : -1;

            if (_brain.Motor.IsWallAhead(_brain.PatrolDir) || _brain.Motor.IsLedgeAhead(_brain.PatrolDir))
            {
                _brain.CurrentPath = null;
                _brain.PatrolDir *= -1;
            }
        }
        else
        {
            if (_brain.Motor.IsLedgeAhead(_brain.PatrolDir) || _brain.Motor.IsWallAhead(_brain.PatrolDir))
            {
                _brain.PatrolDir *= -1;
            }
        }

        _brain.Motor.Move(_brain.Config.patrolSpeed, _brain.Config.patrolAcceleration, _brain.PatrolDir);
    }

    public void Exit() { }
}

public class WormTriggerState : IEnemyState
{
    WormBrain _brain;
    public bool PreserveDirection; 

    public WormTriggerState(WormBrain brain) { _brain = brain; }

    public void Enter()
    {
        Debug.Log("[Worm] Enter Trigger (Windup)");
        _brain.StateTimer = 0;

        if (!PreserveDirection)
        {
            if (_brain.Target)
            {
                float dx = _brain.Target.position.x - _brain.transform.position.x;
                if (Mathf.Abs(dx) > 0.1f)
                    _brain.SpinDirection = new Vector2(Mathf.Sign(dx), 0);
                else
                    _brain.SpinDirection = new Vector2(_brain.transform.localScale.x, 0);
            }
            else
            {
                _brain.SpinDirection = new Vector2(_brain.transform.localScale.x, 0);
            }
        }
        _brain.Motor.Face((int)_brain.SpinDirection.x);

        _brain.Anim?.ResetAllTriggers();
        _brain.Anim?.TriggerAttack();
        _brain.Anim?.TriggerSpinning();

        _brain.Motor.SetFrozen(true);
        PreserveDirection = false; // Reset flag
    }

    public void Tick()
    {
        // Safety timeout
        if (_brain.StateTimer > 3.0f)
        {
            _brain.ChangeState(_brain.SpinState);
            return;
        }

        if (_brain.Anim && _brain.Anim.IsInSpinning())
        {
            _brain.ChangeState(_brain.SpinState);
        }
    }

    public void Exit() { }
}

public class WormSpinState : IEnemyState
{
    WormBrain _brain;
    public WormSpinState(WormBrain brain) { _brain = brain; }

    public void Enter()
    {
        Debug.Log("[Worm] Enter Spinning (Logic)");
        _brain.StateTimer = 0;
        _brain.Motor.SetFrozen(false);
    }

    public void Tick()
    {
        if (_brain.Motor.CheckPlayerAbove(_brain.Config.jumpOverRayHeight))
        {
            _brain.LastHitWasWall = true;
            _brain.ChangeState(_brain.StunState);
            return;
        }

        _brain.Motor.Move(_brain.Config.chargeSpeed, _brain.Config.chargeAcceleration, (int)Mathf.Sign(_brain.SpinDirection.x));

        if (_brain.StateTimer > 0.05f)
        {
            if (_brain.Motor.CheckWallHit(out Vector2 wallNormal))
            {
                _brain.LastHitWasWall = true;
                _brain.ChangeState(_brain.StunState);
                return;
            }

            if (_brain.CheckPlayerHit(out Vector2 away))
            {
                _brain.LastHitWasWall = false;
                _brain.ChangeState(_brain.StunState);
                return;
            }
        }
    }

    public void Exit() { }
}

public class WormStunState : IEnemyState
{
    WormBrain _brain;
    public WormStunState(WormBrain brain) { _brain = brain; }

    public void Enter()
    {
        Debug.Log("[Worm] Enter Stun");
        _brain.StateTimer = 0;
        _brain.Anim?.ResetAllTriggers();
        _brain.Anim?.TriggerStun();
        _brain.Motor.SetFrozen(false);
        _brain.Motor.ApplyDrag(_brain.Config.stunDrag);
    }

    public void Tick()
    {
        bool animFinished = _brain.Anim == null || _brain.Anim.IsStunFinished();
        bool timeout = _brain.StateTimer > 5.0f;

        if (animFinished || timeout)
        {
            if (_brain.ForgetTimer > 0f)
            {
                if (_brain.LastHitWasWall)
                {
                    _brain.SpinDirection = new Vector2(-Mathf.Sign(_brain.SpinDirection.x), 0f);
                    _brain.TriggerState.PreserveDirection = true;
                    _brain.ChangeState(_brain.TriggerState);
                }
                else
                {
                    _brain.ChangeState(_brain.TriggerState);
                }
            }
            else
            {
                _brain.ChangeState(_brain.PatrolState);
            }
        }
    }

    public void Exit()
    {
        _brain.Motor.ResetDrag();
    }
}

public class WormDeadState : IEnemyState
{
    WormBrain _brain;
    public WormDeadState(WormBrain brain) { _brain = brain; }

    public void Enter()
    {
        Debug.Log("[Worm] Dead");
        _brain.Motor.SetFrozen(true);
        _brain.Motor.StopAllCoroutines();

        var prev = _brain.PreviousState;
        if (prev == _brain.SpinState)
            _brain.Anim?.TriggerSpinningDeath();
        else
            _brain.Anim?.TriggerPatrolDeath();

        _brain.enabled = false;
    }

    public void Tick() { }
    public void Exit() { }
}
