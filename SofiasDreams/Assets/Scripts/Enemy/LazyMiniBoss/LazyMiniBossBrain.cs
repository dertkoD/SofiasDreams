using UnityEngine;
using Zenject;

public class LazyMiniBossBrain : BaseEnemyBrain
{
    [Header("Refs")]
    [SerializeField] LazyMiniBossMotor2D _motor;
    [SerializeField] LazyMiniBossAnimatorAdapter _anim;
    [SerializeField] VisionCone2D _vision;
    [SerializeField] Health _health;
    [SerializeField] EnemyPatrolPath _patrolPath;
    [SerializeField] LazyMiniBossConfigSO _config;

    [Header("Shoot")]
    [SerializeField] Transform _shootMuzzle;

    [Header("Attack3")]
    [SerializeField] Transform _attack3MuzzleHorns;

    [Header("Facing")]
    [SerializeField] bool _startFacingLeft = true;

    public LazyMiniBossMotor2D Motor => _motor;
    public LazyMiniBossAnimatorAdapter Anim => _anim;
    public VisionCone2D Vision => _vision;
    public Health Health => _health;
    public EnemyPatrolPath PatrolPath { get => _patrolPath; set => _patrolPath = value; }
    public Transform ShootMuzzle => _shootMuzzle;
    public Transform Attack3MuzzleHorns => _attack3MuzzleHorns;
    public LazyMiniBossConfigSO Config => _config;

    public IHealth IHealth { get; private set; }
    public IEnemyPersistenceService Persist { get; private set; }

    // States
    public LazySleepState SleepState { get; private set; }
    public LazyTriggerAgroState TriggerAgroState { get; private set; }
    public LazyAgroState AgroState { get; private set; }
    public LazyAttackMeleeState AttackMeleeState { get; private set; }
    public LazyAttackShootState AttackShootState { get; private set; }
    public LazyAttackRanged3State AttackRanged3State { get; private set; }
    public LazyTriggerPatrolState TriggerPatrolState { get; private set; }
    public LazyDeadState DeadState { get; private set; }

    // Runtime
    public Transform Player { get; set; }
    public Vector2 LastSeenPos { get; set; }
    public bool HasSeenPlayer { get; set; }
    public float ForgetTimer { get; set; }

    [HideInInspector] public EnemyPatrolPath CurrentPath;
    [HideInInspector] public int PathIndex;
    [HideInInspector] public int PathDir = 1;

    // Zone
    public float ZoneMinX { get; set; }
    public float ZoneMaxX { get; set; }
    public bool ZoneReady { get; set; }

    // Combat
    public float NextMeleeAttackTime { get; set; }
    public float NextShootAttackTime { get; set; }
    public bool UseAttack3Next { get; set; }

    // Pools
    ProjectilePool _shootPool;
    ProjectilePool _attack3Pool;

    // Spawn Meta
    public EnemySpawnMeta SpawnMeta { get; private set; }
    bool _permaKilledSaved;
    int _lastHp;

    [Inject]
    public void Construct(LazyMiniBossConfigSO config, IHealth health, SignalBus bus,
        IEnemyPersistenceService persist, [InjectOptional] PlayerFacade playerFacade)
    {
        _config = config;
        IHealth = health;
        Persist = persist;
        if (playerFacade != null) Player = playerFacade.transform;
        ConstructBase(bus);
    }

    void Awake()
    {
        if (!_motor) _motor = GetComponent<LazyMiniBossMotor2D>();
        if (!_anim) _anim = GetComponent<LazyMiniBossAnimatorAdapter>();
        if (!_vision) _vision = GetComponentInChildren<VisionCone2D>();
        if (!_health) _health = GetComponent<Health>();
        if (IHealth == null && _health) IHealth = _health;

        SleepState = new LazySleepState(this);
        TriggerAgroState = new LazyTriggerAgroState(this);
        AgroState = new LazyAgroState(this);
        AttackMeleeState = new LazyAttackMeleeState(this);
        AttackShootState = new LazyAttackShootState(this);
        AttackRanged3State = new LazyAttackRanged3State(this);
        TriggerPatrolState = new LazyTriggerPatrolState(this);
        DeadState = new LazyDeadState(this);

        SpawnMeta = GetComponent<EnemySpawnMeta>()
                    ?? GetComponentInParent<EnemySpawnMeta>()
                    ?? GetComponentInChildren<EnemySpawnMeta>(true);
    }

    void Start()
    {
        if (_startFacingLeft) _motor.Face(-1);

        if (_config.projectilePrefab)
            _shootPool = new ProjectilePool(_config.projectilePrefab, _config.projectilePoolSize);
        if (_config.attack3ProjectilePrefab)
            _attack3Pool = new ProjectilePool(_config.attack3ProjectilePrefab, _config.attack3ProjectilePoolSize);

        if (PatrolPath == null) PatrolPath = FindNearestPatrolPath();
        CurrentPath = PatrolPath;
        if (CurrentPath != null && CurrentPath.Count > 0)
            PathIndex = FindNearestWaypointIndex(transform.position);

        RecalcZoneBoundsFromPath();
        ChangeState(SleepState);
    }

    void OnEnable()
    {
        if (Health)
        {
            _lastHp = Health.CurrentHP;
            Health.OnHealthChanged += OnHealthChanged;
        }
    }

    void OnDisable()
    {
        if (Health) Health.OnHealthChanged -= OnHealthChanged;
    }

    protected override void Update()
    {
        if (!IHealth.IsAlive)
        {
            if (!_permaKilledSaved)
            {
                TryMarkKilledPermanently();
                _permaKilledSaved = true;
            }
            if (CurrentState != DeadState) ChangeState(DeadState);
            return;
        }

        bool seesPlayer = TrySense(out Transform target);
        if (seesPlayer)
        {
            Player = target;
            LastSeenPos = target.position;
            HasSeenPlayer = true;
            ForgetTimer = Config.agroForgetSeconds;
        }
        else
        {
            if (ForgetTimer > 0) ForgetTimer -= Time.deltaTime;
        }

        base.Update();
        Anim.SetXVelocity(Mathf.Abs(Motor.Velocity.x));
    }

    public void AnimationEvent_SpawnProjectile()
    {
        if (CurrentState == AttackShootState)
            AttackShootState.SpawnProjectile();
        else if (CurrentState == AttackRanged3State)
            AttackRanged3State.SpawnProjectile();
    }

    public void SpawnShootProjectile()
    {
        if (_shootPool == null) return;
        Vector3 pos = _shootMuzzle ? _shootMuzzle.position : transform.position;
        var proj = _shootPool.Get(pos, Quaternion.identity);
        int dir = Motor.IsFacingRight ? 1 : -1;
        proj.Setup(Config.projectileDamage);
        proj.Fire(new Vector2(dir, 0), Config.projectileSpeed);
    }

    public void SpawnAttack3Projectile()
    {
        if (_attack3Pool == null) return;
        Vector3 pos = _attack3MuzzleHorns ? _attack3MuzzleHorns.position : transform.position;
        var proj = _attack3Pool.Get(pos, Quaternion.identity);
        int dir = Motor.IsFacingRight ? 1 : -1;
        proj.Setup(Config.attack3ProjectileDamage);
        proj.Fire(new Vector2(dir, 0), Config.attack3ProjectileSpeed);
    }

    public float ClampXToZone(float x)
    {
        if (!ZoneReady) return x;
        return Mathf.Clamp(x, ZoneMinX, ZoneMaxX);
    }

    public bool IsInsideZone()
    {
        if (!ZoneReady) return true;
        return transform.position.x >= ZoneMinX && transform.position.x <= ZoneMaxX;
    }

    // --- Helpers ---

    void RecalcZoneBoundsFromPath()
    {
        ZoneReady = false;
        if (CurrentPath == null || CurrentPath.Count == 0) return;

        float minX = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;

        for (int i = 0; i < CurrentPath.Count; i++)
        {
            float x = CurrentPath.GetPoint(i).x;
            if (x < minX) minX = x;
            if (x > maxX) maxX = x;
        }

        const float pad = 0.05f;
        ZoneMinX = minX - pad;
        ZoneMaxX = maxX + pad;
        ZoneReady = true;
    }

    void TryMarkKilledPermanently()
    {
        if (Persist == null) return;
        var id = (SpawnMeta != null) ? SpawnMeta.SpawnId : "";
        if (string.IsNullOrEmpty(id)) return;
        if (SpawnMeta != null && SpawnMeta.RespawnMode != EnemyRespawnMode.PersistOnceKilled) return;
        Persist.MarkKilled(id);
    }

    public bool TrySense(out Transform target)
    {
        target = null;
        if (Vision == null) return false;
        return Vision.TryGetClosestTarget(out target);
    }

    void OnHealthChanged()
    {
        if (Health == null) return;
        int current = Health.CurrentHP;

        if (current < _lastHp)
        {
            if (CurrentState == SleepState)
            {
                if (Health.LastHit != null && Health.LastHit.source != null)
                {
                    Transform src = Health.LastHit.source.transform;
                    float dx = src.position.x - transform.position.x;
                    if (Mathf.Abs(dx) > 0.1f)
                        Motor.Face(dx > 0 ? 1 : -1);
                    LastSeenPos = src.position;
                    ChangeState(TriggerAgroState);
                }
            }
        }
        _lastHp = current;
    }

    public void AdvancePathIndex()
    {
        if (CurrentPath == null || CurrentPath.Count <= 1) return;

        if (Config != null && Config.loopPath)
        {
            PathIndex = (PathIndex + 1) % CurrentPath.Count;
            return;
        }

        int next = PathIndex + PathDir;
        if (next >= CurrentPath.Count)
        {
            PathDir = -1;
            next = Mathf.Max(0, CurrentPath.Count - 2);
        }
        else if (next < 0)
        {
            PathDir = 1;
            next = Mathf.Min(1, CurrentPath.Count - 1);
        }
        PathIndex = Mathf.Clamp(next, 0, CurrentPath.Count - 1);
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
        var all = FindObjectsOfType<EnemyPatrolPath>();
        if (all == null || all.Length == 0) return null;

        float best = float.PositiveInfinity;
        EnemyPatrolPath bestPath = null;
        Vector2 pos = transform.position;

        foreach (var p in all)
        {
            if (p == null || p.Count == 0) continue;
            float d = Vector2.Distance(pos, p.transform.position);
            if (d < best) { best = d; bestPath = p; }
        }
        return bestPath;
    }

    void OnDrawGizmos()
    {
        if (Config == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, Config.closeRangeThreshold);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, Config.shootRangeMin);

        if (ZoneReady)
        {
            Gizmos.color = Color.cyan;
            var top = transform.position.y + 2f;
            var bot = transform.position.y - 2f;
            Gizmos.DrawLine(new Vector3(ZoneMinX, bot, 0), new Vector3(ZoneMinX, top, 0));
            Gizmos.DrawLine(new Vector3(ZoneMaxX, bot, 0), new Vector3(ZoneMaxX, top, 0));
        }
    }
}

// --- States ---

public class LazySleepState : IEnemyState
{
    LazyMiniBossBrain _brain;
    public LazySleepState(LazyMiniBossBrain brain) { _brain = brain; }

    public void Enter()
    {
        _brain.Motor.Stop();
    }

    public void Tick()
    {
        if (_brain.TrySense(out Transform target))
        {
            _brain.ChangeState(_brain.TriggerAgroState);
            return;
        }

        if (_brain.Config.canWalkInPatrol)
        {
            if (_brain.CurrentPath == null || _brain.CurrentPath.Count == 0) return;

            Vector3 wp = _brain.CurrentPath.GetPoint(_brain.PathIndex);
            float dist = Vector2.Distance(_brain.transform.position, wp);

            if (dist <= _brain.Config.waypointArriveDistance)
            {
                _brain.AdvancePathIndex();
                _brain.Motor.Stop();
                return;
            }

            float dx = wp.x - _brain.transform.position.x;
            if (Mathf.Abs(dx) < 0.1f)
            {
                _brain.AdvancePathIndex();
                _brain.Motor.Stop();
                return;
            }

            _brain.Motor.Move(Mathf.Sign(dx) * _brain.Config.patrolSpeed);
        }
    }

    public void Exit() { }
}

public class LazyTriggerAgroState : IEnemyState
{
    LazyMiniBossBrain _brain;
    public LazyTriggerAgroState(LazyMiniBossBrain brain) { _brain = brain; }

    public void Enter()
    {
        _brain.Motor.Stop();
        _brain.Anim.TriggerAgro();
    }

    public void Tick()
    {
        if (_brain.Anim.IsInAgroMovement())
            _brain.ChangeState(_brain.AgroState);
    }

    public void Exit() { }
}

public class LazyTriggerPatrolState : IEnemyState
{
    LazyMiniBossBrain _brain;
    public LazyTriggerPatrolState(LazyMiniBossBrain brain) { _brain = brain; }

    public void Enter()
    {
        _brain.Motor.Stop();
        _brain.Anim.TriggerPatrol();
        _brain.HasSeenPlayer = false;
    }

    public void Tick()
    {
        if (_brain.Anim.IsInSleep())
            _brain.ChangeState(_brain.SleepState);
    }

    public void Exit() { }
}

public class LazyAgroState : IEnemyState
{
    LazyMiniBossBrain _brain;
    public LazyAgroState(LazyMiniBossBrain brain) { _brain = brain; }

    public void Enter() { }

    public void Tick()
    {
        bool seesPlayer = _brain.TrySense(out var t);

        if (_brain.ForgetTimer <= 0 && !seesPlayer)
        {
            _brain.ChangeState(_brain.TriggerPatrolState);
            return;
        }

        Vector3 rawTargetPos = seesPlayer ? _brain.Player.position : (Vector3)_brain.LastSeenPos;
        float distToPlayer = Vector2.Distance(_brain.transform.position, rawTargetPos);
        float dxToPlayer = rawTargetPos.x - _brain.transform.position.x;

        if (Mathf.Abs(dxToPlayer) > 0.1f)
            _brain.Motor.Face(dxToPlayer > 0 ? 1 : -1);

        if (_brain.ZoneReady)
        {
            float myX = _brain.transform.position.x;
            if (myX < _brain.ZoneMinX) { _brain.Motor.Move(_brain.Config.agroRunSpeed); return; }
            if (myX > _brain.ZoneMaxX) { _brain.Motor.Move(-_brain.Config.agroRunSpeed); return; }
        }

        if (distToPlayer <= _brain.Config.closeRangeThreshold)
        {
            if (Time.time >= _brain.NextMeleeAttackTime)
            {
                _brain.ChangeState(_brain.AttackMeleeState);
                return;
            }
        }
        else if (seesPlayer && distToPlayer >= _brain.Config.shootRangeMin)
        {
            if (Time.time >= _brain.NextShootAttackTime)
            {
                _brain.ChangeState(_brain.UseAttack3Next
                    ? (IEnemyState)_brain.AttackRanged3State
                    : _brain.AttackShootState);
                return;
            }
        }

        Vector3 clampedTarget = rawTargetPos;
        if (_brain.ZoneReady)
            clampedTarget.x = Mathf.Clamp(rawTargetPos.x, _brain.ZoneMinX, _brain.ZoneMaxX);

        float dxClamped = clampedTarget.x - _brain.transform.position.x;
        float distClamped = Vector2.Distance(_brain.transform.position, clampedTarget);

        if (distClamped > _brain.Config.closeRangeThreshold * 0.8f)
        {
            if (seesPlayer && distToPlayer >= _brain.Config.shootRangeMin &&
                distToPlayer <= _brain.Config.shootRangeMin + 2f &&
                Time.time < _brain.NextShootAttackTime)
            {
                _brain.Motor.Stop();
            }
            else if (Mathf.Abs(dxClamped) > 0.05f)
            {
                _brain.Motor.Move(Mathf.Sign(dxClamped) * _brain.Config.agroRunSpeed);
            }
            else
            {
                _brain.Motor.Stop();
            }
        }
        else
        {
            _brain.Motor.Stop();
        }
    }

    public void Exit() { }
}

public class LazyAttackMeleeState : IEnemyState
{
    LazyMiniBossBrain _brain;
    bool _attack2Triggered;

    public LazyAttackMeleeState(LazyMiniBossBrain brain) { _brain = brain; }

    public void Enter()
    {
        _brain.Motor.Stop();
        _attack2Triggered = false;
        _brain.Anim.SetAttack1(true);
        _brain.NextMeleeAttackTime = Time.time + _brain.Config.meleeAttackCooldown;
    }

    public void Tick()
    {
        if (_brain.Anim.IsInAttack1() && !_attack2Triggered)
        {
            _brain.Anim.SetAttack2(true);
            _attack2Triggered = true;
        }

        if (_brain.Anim.IsInAttack2())
        {
            _brain.Anim.SetAttack1(false);
            _brain.Anim.SetAttack2(false);
        }

        if (_brain.Anim.IsInAgroMovement() && _attack2Triggered)
        {
            _brain.Anim.SetAttack1(false);
            _brain.Anim.SetAttack2(false);
            _brain.ChangeState(_brain.AgroState);
        }
    }

    public void Exit()
    {
        _brain.Anim.SetAttack1(false);
        _brain.Anim.SetAttack2(false);
    }
}

public class LazyAttackShootState : IEnemyState
{
    LazyMiniBossBrain _brain;
    public LazyAttackShootState(LazyMiniBossBrain brain) { _brain = brain; }

    public void Enter()
    {
        _brain.Motor.Stop();
        _brain.Anim.TriggerShoot();
        _brain.NextShootAttackTime = Time.time + _brain.Config.shootAttackCooldown;
        _brain.UseAttack3Next = true;
    }

    public void Tick()
    {
        if (_brain.Anim.IsInAgroMovement())
            _brain.ChangeState(_brain.AgroState);
    }

    public void SpawnProjectile()
    {
        _brain.SpawnShootProjectile();
    }

    public void Exit() { }
}

public class LazyAttackRanged3State : IEnemyState
{
    LazyMiniBossBrain _brain;
    public LazyAttackRanged3State(LazyMiniBossBrain brain) { _brain = brain; }

    public void Enter()
    {
        _brain.Motor.Stop();
        _brain.Anim.SetAttack3(true);
        _brain.NextShootAttackTime = Time.time + _brain.Config.shootAttackCooldown;
        _brain.UseAttack3Next = false;
    }

    public void Tick()
    {
        if (_brain.Anim.IsInAgroMovement())
        {
            _brain.Anim.SetAttack3(false);
            _brain.ChangeState(_brain.AgroState);
        }
    }

    public void SpawnProjectile()
    {
        _brain.SpawnAttack3Projectile();
    }

    public void Exit()
    {
        _brain.Anim.SetAttack3(false);
    }
}

public class LazyDeadState : IEnemyState
{
    LazyMiniBossBrain _brain;
    public LazyDeadState(LazyMiniBossBrain brain) { _brain = brain; }

    public void Enter()
    {
        _brain.Motor.Stop();
        _brain.Anim.TriggerDeath();
        _brain.enabled = false;
    }

    public void Tick() { }
    public void Exit() { }
}
