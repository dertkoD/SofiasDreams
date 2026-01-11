using UnityEngine;
using Zenject;

public class JumpingEnemyBrain : BaseEnemyBrain
{
    [Header("Refs")]
    [SerializeField] JumpingEnemyMotor2D _motor;
    [SerializeField] JumpingEnemyAnimatorAdapter _anim;
    [SerializeField] VisionCone2D _vision;
    [SerializeField] Health _health;
    [SerializeField] EnemyContactDamage _contactDamage;
    [SerializeField] EnemyPatrolPath _patrolPath;

    public JumpingEnemyMotor2D Motor => _motor;
    public JumpingEnemyAnimatorAdapter Anim => _anim;
    public VisionCone2D Vision => _vision;
    public Health Health => _health;
    public EnemyContactDamage ContactDamage => _contactDamage;
    public EnemyPatrolPath PatrolPath { get => _patrolPath; set => _patrolPath = value; }

    public JumpingEnemyConfigSO Config { get; private set; }
    public IHealth IHealth { get; private set; }
    public SignalBus SignalBus { get; private set; }

    // States
    public JumpingPatrolState PatrolState { get; private set; }
    public JumpingAggroTriggerState AggroTriggerState { get; private set; }
    public JumpingAggroState AggroState { get; private set; }
    public JumpingReturnState ReturnState { get; private set; }
    public JumpingDeadState DeadState { get; private set; }

    // Runtime Data
    public Transform Player { get; set; }
    public bool HasSeenPlayerAtLeastOnce { get; set; }
    public Vector2 SpawnPos { get; set; }
    
    // Patrol Runtime
    public EnemyPatrolPath CurrentPath;
    public int PathIndex;
    public int PathDir = 1;
    public bool PatrolJumpHasTarget;
    public Vector2 PatrolJumpTarget;
    public int PatrolDxSignAtJump;
    
    // Return Runtime
    public bool ReturningToRoute;
    public int ReturnTargetIndex;
    public bool ReturnJumpHasTarget;
    public Vector2 ReturnJumpTarget;
    public int ReturnDxSignAtJump;

    // Aggro Runtime
    public float ForgetLeft;
    public bool LostSightTimerRunning;
    public Vector2 LastSeenPos;
    public bool HasLastSeen;
    public int LastChaseDirSign = +1;
    public bool HasChaseDir;

    // Jump Physics
    public bool JumpBool;
    public float NextJumpAt;
    public bool PrevGrounded;
    public float PrevY;
    public float LandingStunUntil;
    public float LastJumpStartedAt;
    
    // Pending Triggers
    public bool PendingAggroTrigger;
    public bool PendingPatrolTrigger;

    // Damage Watch
    public int LastHp = int.MinValue;
    public bool ArmedHpWatch;

    [Inject]
    public void Construct(JumpingEnemyConfigSO config, IHealth health, SignalBus bus)
    {
        Config = config;
        IHealth = health;
        SignalBus = bus;
        ConstructBase(bus);
    }

    void Awake()
    {
        if (!_motor) _motor = GetComponent<JumpingEnemyMotor2D>();
        if (!_health) _health = GetComponent<Health>();
        if (!_vision) _vision = GetComponentInChildren<VisionCone2D>(true);
        if (!_anim) _anim = GetComponentInChildren<JumpingEnemyAnimatorAdapter>(true);
        if (!_contactDamage) _contactDamage = GetComponentInChildren<EnemyContactDamage>(true);
        if (!_patrolPath) _patrolPath = GetComponentInChildren<EnemyPatrolPath>(true);
        if (IHealth == null && _health) IHealth = _health as IHealth;

        SpawnPos = transform.position;

        PatrolState = new JumpingPatrolState(this);
        AggroTriggerState = new JumpingAggroTriggerState(this);
        AggroState = new JumpingAggroState(this);
        ReturnState = new JumpingReturnState(this);
        DeadState = new JumpingDeadState(this);
    }
    
    void Start()
    {
        if (Player == null)
        {
            var pf = FindObjectOfType<PlayerFacade>();
            if (pf != null) Player = pf.transform;
        }
        
        if (PatrolPath == null)
            PatrolPath = FindNearestPatrolPath();

        CurrentPath = PatrolPath;
        if (CurrentPath != null && CurrentPath.Count > 0)
            PathIndex = FindNearestWaypointIndex(SpawnPos);

        PrevGrounded = Motor != null && Motor.IsGrounded;
        PrevY = Motor != null ? Motor.Velocity.y : 0f;
        
        ChangeState(PatrolState);
    }

    void OnEnable()
    {
        if (Health != null) Health.OnHealthChanged += OnHealthChanged;
        if (ContactDamage != null) ContactDamage.OnPlayerContact += OnPlayerContact;
        if (SignalBus != null) SignalBus.Subscribe<PlayerSpawned>(OnPlayerSpawned);
        ArmHpWatch();
    }

    void OnDisable()
    {
        if (Health != null) Health.OnHealthChanged -= OnHealthChanged;
        if (ContactDamage != null) ContactDamage.OnPlayerContact -= OnPlayerContact;
        if (SignalBus != null) SignalBus.TryUnsubscribe<PlayerSpawned>(OnPlayerSpawned);
    }

    protected override void Update()
    {
        if (Config == null || IHealth == null) return;

        if (!IHealth.IsAlive)
        {
            if (Motor != null && !Motor.IsGrounded)
            {
                Motor.StopHorizontal();
                JumpBool = false; 
                Anim.SetJump(false); 
                TickAnimatorParams();
                return;
            }
            if (CurrentState != DeadState) ChangeState(DeadState);
            return;
        }

        if (Anim != null && Motor != null)
        {
            bool inTrigger = Anim.IsInAgroTrigger() || Anim.IsInPatrolTrigger();
            bool stableGround = IsStableOnGround();
            Motor.SetFrozen(inTrigger && stableGround && !JumpBool);
        }

        TickAnimatorParams();

        bool sees = TrySense(out var target);
        if (sees)
        {
            LastSeenPos = target.position;
            HasLastSeen = true;
            float dx = LastSeenPos.x - transform.position.x;
            if (Mathf.Abs(dx) > 0.01f)
            {
                LastChaseDirSign = dx >= 0f ? +1 : -1;
                HasChaseDir = true;
            }

            HasSeenPlayerAtLeastOnce = true;
            if (Player == null)
                Player = target;
        }

        bool aggroTimerActive = PendingAggroTrigger || CurrentState == AggroState || CurrentState == AggroTriggerState;
        if (aggroTimerActive)
        {
             TickGlobalAggroTimer(sees);
        }

        if (PendingAggroTrigger && IsStableOnGround())
        {
            PendingAggroTrigger = false;
            PendingPatrolTrigger = false;
            ChangeState(AggroTriggerState);
        }

        base.Update();
    }

    void TickAnimatorParams()
    {
        if (Anim == null || Motor == null) return;

        bool grounded = Motor.IsGrounded;
        float y = Motor.Velocity.y;

        bool landedByGround = JumpBool && !PrevGrounded && grounded;
        bool landedByVelocity = JumpBool
            && (Time.time - LastJumpStartedAt) > 0.05f
            && PrevY < -0.10f
            && Mathf.Abs(y) < 0.02f;

        if (landedByGround || landedByVelocity)
        {
            JumpBool = false;
            Anim.SetJump(false);

            if (CurrentState is IJumpingState js) js.OnLanded();

            if (PendingAggroTrigger)
            {
                PendingAggroTrigger = false;
                PendingPatrolTrigger = false; 
                ChangeState(AggroTriggerState);
            }
            else if (PendingPatrolTrigger)
            {
                PendingPatrolTrigger = false;
                BeginReturnToPatrol();
            }

            float stun = Config != null ? Mathf.Max(0f, Config.landingStunSeconds) : 0.10f;
            LandingStunUntil = Mathf.Max(LandingStunUntil, Time.time + stun);
            NextJumpAt = Mathf.Max(NextJumpAt, LandingStunUntil);
        }

        float yParam = JumpBool ? (Mathf.Abs(y) + 0.01f) : 0f;

        if (CurrentState == AggroState || CurrentState == AggroTriggerState)
            Anim.SetAttackYVelocity(yParam);
        else
            Anim.SetPatrolYVelocity(yParam);

        PrevGrounded = grounded;
        PrevY = y;
    }

    // --- Logic & Helpers ---

    public interface IJumpingState { void OnLanded(); }

    void TickGlobalAggroTimer(bool sees)
    {
        if (sees)
        {
            ForgetLeft = Config != null ? Config.aggroForgetSeconds : 0f;
            LostSightTimerRunning = false;
        }
        else
        {
            if (!LostSightTimerRunning) LostSightTimerRunning = true;
            else ForgetLeft = Mathf.Max(0f, ForgetLeft - Time.deltaTime);
        }
    }

    public void RequestAggroTrigger()
    {
        if (CurrentState == DeadState) return;

        bool wasAggro = PendingAggroTrigger || CurrentState == AggroState || CurrentState == AggroTriggerState;
        if (!wasAggro)
        {
             if (Config != null) ForgetLeft = Config.aggroForgetSeconds;
        }

        if (JumpBool || !IsStableOnGround())
        {
            PendingAggroTrigger = true;
            PendingPatrolTrigger = false;
            return;
        }

        ChangeState(AggroTriggerState);
    }
    
    public void BeginReturnToPatrol()
    {
        if (CurrentState == DeadState) return;
        if (JumpBool || !IsStableOnGround())
        {
            PendingPatrolTrigger = true;
            return;
        }

        ChangeState(ReturnState);
    }
    
    public bool StartJump(int dirSign, float height, float speed)
    {
        if (Config == null || Motor == null || Anim == null) return false;
        if (Time.time < LandingStunUntil) return false;

        bool ok = Motor.TryJump(dirSign, height, speed);
        if (!ok) return false;

        JumpBool = true;
        LastJumpStartedAt = Time.time;
        Anim.SetJump(true);
        return true;
    }

    public bool IsStableOnGround()
    {
        if (Motor == null || Config == null) return false;
        if (!Motor.IsGrounded) return false;
        if (JumpBool) return false;
        return Mathf.Abs(Motor.Velocity.y) <= Mathf.Max(0f, Config.groundedVelocityEpsilon);
    }
    
    // --- Sensing ---
    
    public bool TrySense(out Transform target)
    {
        target = null;
        if (Vision == null) return false;
        return Vision.TryGetClosestTarget(out target);
    }

    void OnPlayerContact()
    {
        if (CurrentState == DeadState) return;
        if (Player == null)
        {
            var pf = FindObjectOfType<PlayerFacade>();
            if (pf != null) Player = pf.transform;
        }

        if (Player != null)
        {
            LastSeenPos = Player.position;
            HasLastSeen = true;
            HasChaseDir = true;
            
            float dx = LastSeenPos.x - transform.position.x;
            if (Mathf.Abs(dx) > 0.01f)
            {
                LastChaseDirSign = dx >= 0f ? +1 : -1;
            }
        }
        RequestAggroTrigger();
    }
    
    void OnHealthChanged()
    {
        if (Health == null || IHealth == null) return;
        if (!IHealth.IsAlive) return;

        if (ArmedHpWatch)
        {
            int hp = Health.CurrentHP;
            if (LastHp != int.MinValue && hp < LastHp)
            {
                if (Health.LastHit != null && Health.LastHit.source != null)
                {
                    LastSeenPos = Health.LastHit.source.position;
                    HasLastSeen = true;
                    HasChaseDir = true;
                    float dx = LastSeenPos.x - transform.position.x;
                    if (Mathf.Abs(dx) > 0.01f)
                    {
                         LastChaseDirSign = dx >= 0f ? +1 : -1;
                    }
                }
                RequestAggroTrigger();
            }
            LastHp = hp;
        }
    }
    
    void ArmHpWatch()
    {
        if (Health == null) return;
        LastHp = Health.CurrentHP;
        ArmedHpWatch = true;
    }

    void OnPlayerSpawned(PlayerSpawned s)
    {
        if (s.facade != null) Player = s.facade.transform;
    }
    
    // --- Path ---
    
    public void AdvancePathIndex()
    {
        if (CurrentPath == null || CurrentPath.Count <= 1) return;
        if (Config != null && Config.loopPath)
        {
            PathIndex = (PathIndex + 1) % CurrentPath.Count;
            return;
        }
        int next = PathIndex + PathDir;
        if (next >= CurrentPath.Count || next < 0)
        {
            PathDir *= -1;
            next = Mathf.Clamp(PathIndex + PathDir, 0, CurrentPath.Count - 1);
        }
        PathIndex = next;
    }

    public int FindNearestWaypointIndex(Vector2 pos)
    {
        if (CurrentPath == null || CurrentPath.Count == 0) return 0;
        int bestIndex = 0;
        float best = float.PositiveInfinity;
        for (int i = 0; i < CurrentPath.Count; i++)
        {
            Vector2 p = CurrentPath.GetPoint(i);
            float d = (p - pos).sqrMagnitude;
            if (d < best) { best = d; bestIndex = i; }
        }
        return bestIndex;
    }
    
    EnemyPatrolPath FindNearestPatrolPath()
    {
        var all = FindObjectsOfType<EnemyPatrolPath>(true);
        if (all == null || all.Length == 0) return null;

        float best = float.PositiveInfinity;
        EnemyPatrolPath bestPath = null;
        Vector2 pos = transform.position;
        float radius = Config != null ? Mathf.Max(0f, Config.patrolPathSearchRadius) : 100f;

        for (int i = 0; i < all.Length; i++)
        {
            var p = all[i];
            if (p == null || p.Count == 0) continue;
            float d = Vector2.Distance(pos, p.transform.position);
            if (d <= radius && d < best)
            {
                best = d;
                bestPath = p;
            }
        }
        return bestPath;
    }
    
    public float CalculateJumpDistance(float height, float speed)
    {
        if (Motor == null || Motor.Rigidbody == null) return 0f;
        float g = Mathf.Abs(Physics2D.gravity.y * Motor.Rigidbody.gravityScale);
        if (g <= 0.0001f) return 0f;
        float t = 2f * Mathf.Sqrt(2f * height / g);
        return speed * t;
    }
    
    public int GetPatrolDirectionSign(out Vector2 target, out bool hasTarget)
    {
        if (CurrentPath == null || CurrentPath.Count == 0)
        {
            target = SpawnPos;
            hasTarget = false;
            return transform.localScale.x >= 0f ? +1 : -1;
        }

        Vector3 t = CurrentPath.GetPoint(PathIndex);
        float dx = t.x - transform.position.x;
        if (Mathf.Abs(dx) < 0.01f) dx = transform.localScale.x;

        target = t;
        hasTarget = true;
        return dx >= 0f ? +1 : -1;
    }

    public int GetAggroDirectionSign()
    {
        if (HasSeenPlayerAtLeastOnce && Player != null)
        {
            float dx = Player.position.x - transform.position.x;
            if (Mathf.Abs(dx) < 0.01f) dx = transform.localScale.x;
            int sign = dx >= 0f ? +1 : -1;
            LastChaseDirSign = sign;
            HasChaseDir = true;
            return sign;
        }
        if (HasLastSeen)
        {
            float dx = LastSeenPos.x - transform.position.x;
            if (Mathf.Abs(dx) < 0.01f) dx = transform.localScale.x;
            int sign = dx >= 0f ? +1 : -1;
            LastChaseDirSign = sign;
            HasChaseDir = true;
            return sign;
        }
        return HasChaseDir ? LastChaseDirSign : (transform.localScale.x >= 0f ? +1 : -1);
    }
}

// --- States ---

public class JumpingPatrolState : IEnemyState, JumpingEnemyBrain.IJumpingState
{
    JumpingEnemyBrain _brain;
    public JumpingPatrolState(JumpingEnemyBrain brain) { _brain = brain; }

    public void Enter() { }

    public void Tick()
    {
        if (_brain.Config == null || _brain.Motor == null) return;
        
        // Check Aggro
        if (_brain.TrySense(out var t)) { _brain.RequestAggroTrigger(); return; } // Actually Brain.Update calls logic too, but we need to break
        // Actually Brain checks `sees` and updates timers. RequestAggroTrigger is triggered by events.
        // But in original TickPatrol: if (sees) { RequestAggroTrigger(); break; }
        // So we should check here too or rely on Brain?
        // Brain's Update: "bool sees = TrySense... if(sees)..."
        // But Brain DOES NOT automatically switch to Aggro unless PendingAggroTrigger.
        // We need to request it here.
        if (_brain.Vision && _brain.Vision.TryGetClosestTarget(out var _))
        {
            _brain.RequestAggroTrigger();
            return;
        }

        if (_brain.PendingAggroTrigger) return;
        if (!_brain.Motor.IsGrounded) return;
        if (Time.time < _brain.LandingStunUntil) return;
        if (Time.time < _brain.NextJumpAt) return;

        // If at waypoint
        Vector2 cur = _brain.CurrentPath.GetPoint(_brain.PathIndex);
        float arrive = Mathf.Max(0.01f, _brain.Config.waypointArriveDistance);
        if (Vector2.Distance(_brain.transform.position, cur) <= arrive)
            _brain.AdvancePathIndex();

        // Check Jump Distance
        if (_brain.CurrentPath != null && _brain.CurrentPath.Count > 0)
        {
            Vector2 targetPt = _brain.CurrentPath.GetPoint(_brain.PathIndex);
            float checkH = _brain.Config.patrolJumpHeight;
            float checkS = _brain.Config.patrolJumpHorizontalSpeed;
            float jumpDist = _brain.CalculateJumpDistance(checkH, checkS);
            float distX = Mathf.Abs(targetPt.x - _brain.transform.position.x);

            if (distX < jumpDist)
            {
                _brain.AdvancePathIndex();
                return;
            }
        }

        int dir = _brain.GetPatrolDirectionSign(out var patrolTarget, out bool hasTarget);
        float h = _brain.Config.patrolJumpHeight;
        float s = _brain.Config.patrolJumpHorizontalSpeed;

        if (hasTarget)
        {
            _brain.PatrolJumpHasTarget = true;
            _brain.PatrolJumpTarget = patrolTarget;
            float dx = patrolTarget.x - _brain.transform.position.x;
            _brain.PatrolDxSignAtJump = Mathf.Abs(dx) < 0.001f ? (_brain.transform.localScale.x >= 0f ? +1 : -1) : (dx >= 0f ? +1 : -1);
        }
        else
        {
            _brain.PatrolJumpHasTarget = false;
        }

        if (_brain.StartJump(dir, h, s))
            _brain.NextJumpAt = Time.time + _brain.Config.patrolJumpCooldown;
    }

    public void OnLanded()
    {
        if (!_brain.PatrolJumpHasTarget || _brain.CurrentPath == null || _brain.CurrentPath.Count == 0 || _brain.Config == null) return;
        
        float arrive = Mathf.Max(0.01f, _brain.Config.waypointArriveDistance);
        float dist = Vector2.Distance((Vector2)_brain.transform.position, _brain.PatrolJumpTarget);
        float dxNow = _brain.PatrolJumpTarget.x - _brain.transform.position.x;
        int dxSignNow = Mathf.Abs(dxNow) < 0.001f ? _brain.PatrolDxSignAtJump : (dxNow >= 0f ? +1 : -1);

        if (dist <= arrive || dxSignNow != _brain.PatrolDxSignAtJump)
            _brain.AdvancePathIndex();

        _brain.PatrolJumpHasTarget = false;
    }
    
    public void Exit() { }
}

public class JumpingAggroTriggerState : IEnemyState
{
    JumpingEnemyBrain _brain;
    public JumpingAggroTriggerState(JumpingEnemyBrain brain) { _brain = brain; }

    public void Enter()
    {
        _brain.LostSightTimerRunning = false;
        _brain.Motor?.StopAll();
        
        if (_brain.HasLastSeen && _brain.Motor != null)
        {
             float dx = _brain.LastSeenPos.x - _brain.transform.position.x;
             if (Mathf.Abs(dx) > 0.01f)
                 _brain.Motor.Face(dx >= 0 ? 1 : -1);
        }

        _brain.JumpBool = false;
        _brain.Anim?.SetJump(false);
        _brain.Anim?.TriggerAgro();
    }

    public void Tick()
    {
        if (_brain.Anim.IsInAttackLoop())
        {
            _brain.ChangeState(_brain.AggroState);
        }
    }
    public void Exit() 
    {
        // When entering Aggro state, allow immediate jump
        _brain.NextJumpAt = Time.time;
        if (_brain.Config != null) _brain.ForgetLeft = _brain.Config.aggroForgetSeconds;
        _brain.LostSightTimerRunning = false;
    }
}

public class JumpingAggroState : IEnemyState
{
    JumpingEnemyBrain _brain;
    public JumpingAggroState(JumpingEnemyBrain brain) { _brain = brain; }

    public void Enter() { }

    public void Tick()
    {
        if (_brain.ForgetLeft <= 0f)
        {
            if (_brain.Motor.IsGrounded) _brain.BeginReturnToPatrol();
            else _brain.PendingPatrolTrigger = true;
            return;
        }

        if (!_brain.Motor.IsGrounded) return;
        if (Time.time < _brain.LandingStunUntil) return;
        if (Time.time < _brain.NextJumpAt) return;

        int dir = _brain.GetAggroDirectionSign();
        float h = _brain.Config.aggroJumpHeight;
        float s = _brain.Config.aggroJumpHorizontalSpeed;

        if (_brain.StartJump(dir, h, s))
            _brain.NextJumpAt = Time.time + _brain.Config.aggroJumpCooldown;
    }
    public void Exit() { }
}

public class JumpingReturnState : IEnemyState, JumpingEnemyBrain.IJumpingState
{
    JumpingEnemyBrain _brain;
    public JumpingReturnState(JumpingEnemyBrain brain) { _brain = brain; }

    public void Enter()
    {
        _brain.HasLastSeen = false;
        _brain.HasChaseDir = false;
        _brain.HasSeenPlayerAtLeastOnce = false;
        _brain.LostSightTimerRunning = false;
        _brain.JumpBool = false;

        if (_brain.CurrentPath == null || _brain.CurrentPath.Count == 0)
        {
            _brain.ReturningToRoute = false;
        }
        else
        {
            _brain.ReturningToRoute = true;
            _brain.ReturnTargetIndex = _brain.FindNearestWaypointIndex(_brain.transform.position);
            _brain.PathIndex = _brain.ReturnTargetIndex;
        }

        _brain.Anim?.SetJump(false);
        _brain.Anim?.TriggerPatrol();
        _brain.NextJumpAt = Time.time + 0.05f;
    }

    public void Tick()
    {
        if (_brain.Vision && _brain.Vision.TryGetClosestTarget(out var _)) { _brain.RequestAggroTrigger(); return; }

        if (_brain.Anim != null && _brain.Anim.IsInPatrolTrigger()) return;

        // Check if back on route
        if (_brain.CurrentPath != null && _brain.CurrentPath.Count > 0 && _brain.Motor.IsGrounded)
        {
            float minX = float.MaxValue;
            float maxX = float.MinValue;
            for (int i = 0; i < _brain.CurrentPath.Count; i++)
            {
                float px = _brain.CurrentPath.GetPoint(i).x;
                if (px < minX) minX = px;
                if (px > maxX) maxX = px;
            }

            if (_brain.transform.position.x >= minX && _brain.transform.position.x <= maxX)
            {
                _brain.ReturningToRoute = false;
                _brain.PathIndex = _brain.FindNearestWaypointIndex(_brain.transform.position);
                _brain.ChangeState(_brain.PatrolState);
                return;
            }
        }

        // Return logic
        if (_brain.CurrentPath != null && _brain.CurrentPath.Count > 0 && _brain.ReturningToRoute)
        {
            Vector2 dst = _brain.CurrentPath.GetPoint(_brain.ReturnTargetIndex);

            if (!_brain.Motor.IsGrounded && !_brain.Motor.IsFrozen)
            {
                int dirAir = dst.x >= _brain.transform.position.x ? +1 : -1;
                _brain.Motor.SetAirDesiredVX(dirAir * Mathf.Max(0f, _brain.Config.patrolJumpHorizontalSpeed));
            }

            float arrive = Mathf.Max(0.01f, _brain.Config.waypointArriveDistance);
            if (_brain.Motor.IsGrounded && Vector2.Distance(_brain.transform.position, dst) <= arrive)
            {
                _brain.ReturningToRoute = false;
                _brain.AdvancePathIndex();
                _brain.ChangeState(_brain.PatrolState);
                return;
            }

            if (!_brain.Motor.IsGrounded) return;
            if (Time.time < _brain.LandingStunUntil) return;
            if (Time.time < _brain.NextJumpAt) return;

            int dir = (dst.x >= _brain.transform.position.x) ? +1 : -1;
            float h = _brain.Config.patrolJumpHeight;
            float s = _brain.Config.patrolJumpHorizontalSpeed;

            _brain.ReturnJumpHasTarget = true;
            _brain.ReturnJumpTarget = dst;
            float dx = dst.x - _brain.transform.position.x;
            _brain.ReturnDxSignAtJump = Mathf.Abs(dx) < 0.001f
                ? (_brain.transform.localScale.x >= 0f ? +1 : -1)
                : (dx >= 0f ? +1 : -1);

            if (_brain.StartJump(dir, h, s))
                _brain.NextJumpAt = Time.time + _brain.Config.patrolJumpCooldown;
        }
    }

    public void OnLanded()
    {
        if (!_brain.ReturningToRoute || !_brain.ReturnJumpHasTarget || _brain.CurrentPath == null || _brain.CurrentPath.Count == 0 || _brain.Config == null)
            return;

        float arrive = Mathf.Max(0.01f, _brain.Config.waypointArriveDistance);
        float dist = Vector2.Distance((Vector2)_brain.transform.position, _brain.ReturnJumpTarget);
        float dxNow = _brain.ReturnJumpTarget.x - _brain.transform.position.x;
        int dxSignNow = Mathf.Abs(dxNow) < 0.001f ? _brain.ReturnDxSignAtJump : (dxNow >= 0f ? +1 : -1);

        bool reached = dist <= arrive || dxSignNow != _brain.ReturnDxSignAtJump;
        _brain.ReturnJumpHasTarget = false;

        if (reached)
        {
            _brain.ReturningToRoute = false;
            _brain.PathIndex = _brain.ReturnTargetIndex;
            _brain.AdvancePathIndex();
            _brain.ChangeState(_brain.PatrolState);
        }
    }
    
    public void Exit() { }
}

public class JumpingDeadState : IEnemyState
{
    JumpingEnemyBrain _brain;
    public JumpingDeadState(JumpingEnemyBrain brain) { _brain = brain; }

    public void Enter()
    {
        var prev = _brain.PreviousState;
        _brain.Motor?.StopHorizontal();
        if (_brain.Anim != null)
        {
            _brain.Anim.SetJump(false);
            bool fromAttack = prev == _brain.AggroState || prev == _brain.AggroTriggerState || _brain.Anim.IsInAttackLoop();
            if (fromAttack) _brain.Anim.TriggerDeathFromAttack();
            else _brain.Anim.TriggerDeathFromPatrol();
        }
        _brain.enabled = false;
    }
    public void Tick() { }
    public void Exit() { }
}
