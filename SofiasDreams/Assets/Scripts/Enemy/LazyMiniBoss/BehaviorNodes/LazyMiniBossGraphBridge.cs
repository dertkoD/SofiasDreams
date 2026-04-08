using Unity.Behavior;
using UnityEngine;

public class LazyMiniBossGraphBridge : MonoBehaviour
{
    [Header("Refs (auto-filled if empty)")]
    [SerializeField] BehaviorGraphAgent _graphAgent;
    [SerializeField] LazyMiniBossMotor2D _motor;
    [SerializeField] LazyMiniBossAnimatorAdapter _anim;
    [SerializeField] VisionCone2D _vision;
    [SerializeField] Health _health;
    [SerializeField] LazyMiniBossConfigSO _config;
    [SerializeField] EnemyPatrolPath _patrolPath;

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
    public LazyMiniBossConfigSO Config => _config;
    public EnemyPatrolPath PatrolPath => _patrolPath;
    public Transform ShootMuzzle => _shootMuzzle;
    public GameObjectPooler ShootPool => _shootPool;
    public Transform Attack3Muzzle => _attack3Muzzle;
    public GameObjectPooler Attack3Pool => _attack3Pool;
    public BehaviorGraphAgent GraphAgent => _graphAgent;

    public Transform Player { get; set; }
    public Vector2 LastSeenPos { get; set; }
    public bool HasSeenPlayer { get; set; }
    public float ForgetTimer { get; set; }
    public float NextMeleeAttackTime { get; set; }
    public float NextShootAttackTime { get; set; }
    public float NextAttack3Time { get; set; }
    public bool LastRangedWasShoot { get; set; }

    public float ZoneMinX { get; private set; }
    public float ZoneMaxX { get; private set; }
    public bool ZoneReady { get; private set; }

    void Awake()
    {
        if (!_graphAgent) _graphAgent = GetComponent<BehaviorGraphAgent>();
        if (!_motor) _motor = GetComponent<LazyMiniBossMotor2D>();
        if (!_anim) _anim = GetComponent<LazyMiniBossAnimatorAdapter>();
        if (!_vision) _vision = GetComponentInChildren<VisionCone2D>();
        if (!_health) _health = GetComponent<Health>();

        EnsureAnimEventForwarder();
    }

    void Start()
    {
        if (_config != null && _config.spawnFacingLeft)
            _motor.Face(-1);

        RecalcZoneBoundsFromPath();
    }

    void Update()
    {
        UpdateVision();
        Anim.SetXVelocity(Mathf.Abs(Motor.Velocity.x));
    }

    /// <summary>
    /// Called from EnemyFacade.SetPatrolPath at runtime.
    /// </summary>
    public void SetPatrolPath(EnemyPatrolPath path)
    {
        _patrolPath = path;
        RecalcZoneBoundsFromPath();
    }

    void UpdateVision()
    {
        if (_vision != null && _vision.TryGetClosestTarget(out Transform target))
        {
            Player = target;
            LastSeenPos = target.position;
            HasSeenPlayer = true;
            if (_config != null)
                ForgetTimer = _config.agroForgetSeconds;
        }
        else
        {
            if (ForgetTimer > 0) ForgetTimer -= Time.deltaTime;
        }
    }

    public bool SeesPlayer()
    {
        if (_vision == null) return false;
        return _vision.TryGetClosestTarget(out _);
    }

    public float DistanceToPlayer()
    {
        if (Player == null) return float.MaxValue;
        return Vector2.Distance(transform.position, Player.position);
    }

    public void FacePlayer()
    {
        if (Player == null) return;
        float dx = Player.position.x - transform.position.x;
        if (Mathf.Abs(dx) > 0.1f)
            _motor.Face(dx > 0 ? 1 : -1);
    }

    public void ClampToZone()
    {
        if (!ZoneReady) return;
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, ZoneMinX, ZoneMaxX);
        transform.position = pos;
    }

    // --- Projectile spawning ---

    public void SpawnShootProjectile()
    {
        SpawnProjectileInternal(
            _shootMuzzle, _shootPool,
            _config.projectilePrefab, _config.projectileDamage, _config.projectileSpeed);
    }

    public void SpawnAttack3Projectile()
    {
        SpawnProjectileInternal(
            _attack3Muzzle, _attack3Pool,
            _config.attack3ProjectilePrefab, _config.attack3ProjectileDamage, _config.attack3ProjectileSpeed);
    }

    void SpawnProjectileInternal(Transform muzzle, GameObjectPooler pool,
        GameObject prefab, int damage, float speed)
    {
        if (prefab == null && pool == null) return;

        Vector3 spawnPos = muzzle ? muzzle.position : transform.position;
        int dir = _motor.IsFacingRight ? 1 : -1;
        Vector2 direction = new Vector2(dir, 0);
        Quaternion rot = Quaternion.FromToRotation(Vector3.right, (Vector3)direction);

        GameObject go = pool != null
            ? pool.Get(spawnPos, rot)
            : Instantiate(prefab, spawnPos, rot);

        if (go)
        {
            var proj = go.GetComponent<FistProjectile>();
            if (proj)
            {
                proj.Setup(damage);
                proj.Fire(direction, speed);
            }
        }
    }

    // --- Animation event receivers (called on THIS object) ---

    public void AnimationEvent_SpawnShootProjectile() => SpawnShootProjectile();
    public void AnimationEvent_SpawnAttack3Projectile() => SpawnAttack3Projectile();
    public void AnimationEvent_SpawnProjectile()
    {
        if (Anim.IsInShoot()) SpawnShootProjectile();
        else if (Anim.IsInAttack3()) SpawnAttack3Projectile();
    }

    // --- Zone ---

    void RecalcZoneBoundsFromPath()
    {
        ZoneReady = false;
        if (_patrolPath == null || _patrolPath.Count == 0) return;

        float minX = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;

        for (int i = 0; i < _patrolPath.Count; i++)
        {
            float x = _patrolPath.GetPoint(i).x;
            if (x < minX) minX = x;
            if (x > maxX) maxX = x;
        }

        const float pad = 0.05f;
        ZoneMinX = minX - pad;
        ZoneMaxX = maxX + pad;
        ZoneReady = true;
    }

    /// <summary>
    /// The Animator lives on a child object. Animation events fire on that child.
    /// This auto-adds a small forwarder script to the child so events reach this bridge.
    /// </summary>
    void EnsureAnimEventForwarder()
    {
        Animator animator = GetComponentInChildren<Animator>();
        if (animator == null || animator.gameObject == gameObject) return;

        var fwd = animator.gameObject.GetComponent<LazyBossAnimEventForwarder>();
        if (fwd == null)
            fwd = animator.gameObject.AddComponent<LazyBossAnimEventForwarder>();
        fwd.SetBridge(this);
    }
}
