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
    [SerializeField] GameObjectPooler _shootPool;

    [Header("Attack3")]
    [SerializeField] Transform _attack3Muzzle;
    [SerializeField] GameObjectPooler _attack3Pool;

    public LazyMiniBossMotor2D Motor => _motor;
    public LazyMiniBossAnimatorAdapter Anim => _anim;
    public VisionCone2D Vision => _vision;
    public Health Health => _health;
    public EnemyPatrolPath PatrolPath { get => _patrolPath; set => _patrolPath = value; }
    public Transform ShootMuzzle => _shootMuzzle;
    public GameObjectPooler ShootPool => _shootPool;
    public Transform Attack3Muzzle => _attack3Muzzle;
    public GameObjectPooler Attack3Pool => _attack3Pool;
    public LazyMiniBossConfigSO Config => _config;

    public IHealth IHealth { get; private set; }
    public IEnemyPersistenceService Persist { get; private set; }

    // States
    public LazyPatrolState PatrolState { get; private set; }
    public LazyTriggerAgroState TriggerAgroState { get; private set; }
    public LazyAgroState AgroState { get; private set; }
    public LazyAttackMeleeState AttackMeleeState { get; private set; }
    public LazyAttackShootState AttackShootState { get; private set; }
    public LazyAttackThreeState AttackThreeState { get; private set; }
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
    public float NextAttack3Time { get; set; }
    public bool LastRangedWasShoot { get; set; }

    // Spawn Meta
    public EnemySpawnMeta SpawnMeta { get; private set; }
    bool _permaKilledSaved;
    int _lastHp;

    [Inject]
    public void Construct(LazyMiniBossConfigSO config, IHealth health, SignalBus bus, IEnemyPersistenceService persist, [InjectOptional] PlayerFacade playerFacade)
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

        PatrolState = new LazyPatrolState(this);
        TriggerAgroState = new LazyTriggerAgroState(this);
        AgroState = new LazyAgroState(this);
        AttackMeleeState = new LazyAttackMeleeState(this);
        AttackShootState = new LazyAttackShootState(this);
        AttackThreeState = new LazyAttackThreeState(this);
        TriggerPatrolState = new LazyTriggerPatrolState(this);
        DeadState = new LazyDeadState(this);

        SpawnMeta = GetComponent<EnemySpawnMeta>()
                     ?? GetComponentInParent<EnemySpawnMeta>()
                     ?? GetComponentInChildren<EnemySpawnMeta>(true);
    }

    void Start()
    {
        if (Config != null && Config.spawnFacingLeft)
            Motor.Face(-1);

        if (PatrolPath == null) PatrolPath = FindNearestPatrolPath();
        CurrentPath = PatrolPath;
        if (CurrentPath != null && CurrentPath.Count > 0)
        {
            PathIndex = FindNearestWaypointIndex(transform.position);
        }

        RecalcZoneBoundsFromPath();

        ChangeState(PatrolState);
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

    // Animation events — called from clips
    public void AnimationEvent_SpawnShootProjectile()
    {
        if (CurrentState == AttackShootState)
            AttackShootState.SpawnProjectile();
    }

    public void AnimationEvent_SpawnAttack3Projectile()
    {
        if (CurrentState == AttackThreeState)
            AttackThreeState.SpawnProjectile();
    }

    // Keep backward compat for old clips
    public void AnimationEvent_SpawnProjectile()
    {
        if (CurrentState == AttackShootState)
            AttackShootState.SpawnProjectile();
        else if (CurrentState == AttackThreeState)
            AttackThreeState.SpawnProjectile();
    }

    // --- Zone enforcement ---

    public void ClampToZone()
    {
        if (!ZoneReady) return;
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, ZoneMinX, ZoneMaxX);
        transform.position = pos;
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

        Debug.Log($"[PERSIST] MarkKilled (LazyMiniBoss): {id}");
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
            if (CurrentState == PatrolState)
            {
                if (Health.LastHit != null && Health.LastHit.source != null)
                {
                    Transform src = Health.LastHit.source.transform;
                    float dx = src.position.x - transform.position.x;
                    if (Mathf.Abs(dx) > 0.1f)
                    {
                        Motor.Face(dx > 0 ? 1 : -1);
                    }
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
    }
}

// --- States ---

public class LazyPatrolState : IEnemyState
{
    LazyMiniBossBrain _brain;
    public LazyPatrolState(LazyMiniBossBrain brain) { _brain = brain; }

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

        _brain.ClampToZone();

        if (!_brain.Config.patrolWalk)
            return;

        if (_brain.CurrentPath == null || _brain.CurrentPath.Count == 0) return;

        Vector3 targetPos = _brain.CurrentPath.GetPoint(_brain.PathIndex);
        float dist = Vector2.Distance(_brain.transform.position, targetPos);

        if (dist <= _brain.Config.waypointArriveDistance)
        {
            _brain.AdvancePathIndex();
            _brain.Motor.Stop();
            return;
        }

        float dx = targetPos.x - _brain.transform.position.x;
        if (Mathf.Abs(dx) < 0.1f)
        {
            _brain.AdvancePathIndex();
            _brain.Motor.Stop();
            return;
        }

        _brain.Motor.Move(Mathf.Sign(dx) * _brain.Config.patrolSpeed);
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
        {
            _brain.ChangeState(_brain.AgroState);
        }
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
        bool inPatrol = _brain.Config.patrolWalk
            ? _brain.Anim.IsInPatrolMovement()
            : _brain.Anim.IsInSleep() || _brain.Anim.IsInPatrolMovement();

        if (inPatrol)
        {
            _brain.PathIndex = _brain.FindNearestWaypointIndex(_brain.transform.position);
            _brain.ChangeState(_brain.PatrolState);
        }
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

        Vector3 moveTargetPos = rawTargetPos;
        if (_brain.ZoneReady)
            moveTargetPos.x = Mathf.Clamp(rawTargetPos.x, _brain.ZoneMinX, _brain.ZoneMaxX);

        float distToMoveTarget = Vector2.Distance(_brain.transform.position, moveTargetPos);
        float dxToMoveTarget = moveTargetPos.x - _brain.transform.position.x;

        if (Mathf.Abs(dxToPlayer) > 0.1f)
            _brain.Motor.Face(dxToPlayer > 0 ? 1 : -1);

        _brain.ClampToZone();

        if (_brain.ZoneReady)
        {
            if (_brain.transform.position.x < _brain.ZoneMinX)
            {
                _brain.Motor.Move(_brain.Config.agroRunSpeed);
                return;
            }
            if (_brain.transform.position.x > _brain.ZoneMaxX)
            {
                _brain.Motor.Move(-_brain.Config.agroRunSpeed);
                return;
            }
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
            if (!_brain.LastRangedWasShoot && Time.time >= _brain.NextShootAttackTime)
            {
                _brain.ChangeState(_brain.AttackShootState);
                return;
            }
            if (_brain.LastRangedWasShoot && Time.time >= _brain.NextAttack3Time)
            {
                _brain.ChangeState(_brain.AttackThreeState);
                return;
            }
            if (Time.time >= _brain.NextShootAttackTime)
            {
                _brain.ChangeState(_brain.AttackShootState);
                return;
            }
        }

        if (distToMoveTarget > _brain.Config.closeRangeThreshold * 0.8f)
        {
            bool waitingForRangedCooldown = seesPlayer
                && distToPlayer >= _brain.Config.shootRangeMin
                && distToPlayer <= _brain.Config.shootRangeMin + 2f
                && Time.time < _brain.NextShootAttackTime
                && Time.time < _brain.NextAttack3Time;

            if (waitingForRangedCooldown)
            {
                _brain.Motor.Stop();
            }
            else
            {
                if (Mathf.Abs(dxToMoveTarget) > 0.05f)
                    _brain.Motor.Move(Mathf.Sign(dxToMoveTarget) * _brain.Config.agroRunSpeed);
                else
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
        if (_brain.Anim.IsInAttack1())
        {
            if (!_attack2Triggered)
            {
                _brain.Anim.SetAttack2(true);
                _attack2Triggered = true;
            }
        }

        if (_brain.Anim.IsInAttack2())
        {
            _brain.Anim.SetAttack1(false);
            _brain.Anim.SetAttack2(false);
        }

        if (_brain.Anim.IsInAgroMovement() && _attack2Triggered)
        {
            _brain.ChangeState(_brain.AgroState);
            _brain.Anim.SetAttack1(false);
            _brain.Anim.SetAttack2(false);
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
        _brain.LastRangedWasShoot = true;
    }

    public void Tick()
    {
        if (_brain.Anim.IsInAgroMovement())
        {
            _brain.ChangeState(_brain.AgroState);
        }
    }

    public void SpawnProjectile()
    {
        Transform muzzle = _brain.ShootMuzzle;
        GameObjectPooler pool = _brain.ShootPool;
        var cfg = _brain.Config;
        if (cfg.projectilePrefab == null && pool == null) return;

        Vector3 spawnPos = muzzle ? muzzle.position : _brain.transform.position;
        int dir = _brain.Motor.IsFacingRight ? 1 : -1;
        Vector2 direction = new Vector2(dir, 0);
        Quaternion rot = Quaternion.FromToRotation(Vector3.right, (Vector3)direction);

        GameObject go = null;
        if (pool != null)
        {
            go = pool.Get(spawnPos, rot);
        }
        else
        {
            go = Object.Instantiate(cfg.projectilePrefab, spawnPos, rot);
        }

        if (go)
        {
            var proj = go.GetComponent<FistProjectile>();
            if (proj)
            {
                proj.Setup(cfg.projectileDamage);
                proj.Fire(direction, cfg.projectileSpeed);
            }
        }
    }

    public void Exit() { }
}

public class LazyAttackThreeState : IEnemyState
{
    LazyMiniBossBrain _brain;
    public LazyAttackThreeState(LazyMiniBossBrain brain) { _brain = brain; }

    public void Enter()
    {
        _brain.Motor.Stop();
        _brain.Anim.TriggerAttack3();
        _brain.NextAttack3Time = Time.time + _brain.Config.attack3Cooldown;
        _brain.LastRangedWasShoot = false;
    }

    public void Tick()
    {
        if (_brain.Anim.IsInAgroMovement())
        {
            _brain.ChangeState(_brain.AgroState);
        }
    }

    public void SpawnProjectile()
    {
        Transform muzzle = _brain.Attack3Muzzle;
        GameObjectPooler pool = _brain.Attack3Pool;
        var cfg = _brain.Config;
        if (cfg.attack3ProjectilePrefab == null && pool == null) return;

        Vector3 spawnPos = muzzle ? muzzle.position : _brain.transform.position;
        int dir = _brain.Motor.IsFacingRight ? 1 : -1;
        Vector2 direction = new Vector2(dir, 0);
        Quaternion rot = Quaternion.FromToRotation(Vector3.right, (Vector3)direction);

        GameObject go = null;
        if (pool != null)
        {
            go = pool.Get(spawnPos, rot);
        }
        else
        {
            go = Object.Instantiate(cfg.attack3ProjectilePrefab, spawnPos, rot);
        }

        if (go)
        {
            var proj = go.GetComponent<FistProjectile>();
            if (proj)
            {
                proj.Setup(cfg.attack3ProjectileDamage);
                proj.Fire(direction, cfg.attack3ProjectileSpeed);
            }
        }
    }

    public void Exit() { }
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
