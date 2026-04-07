using UnityEngine;

public class BullBehaviorBridge : MonoBehaviour
{
    [SerializeField] LazyMiniBossBrain _brain;
    [SerializeField] LazyMiniBossMotor2D _motor;
    [SerializeField] LazyMiniBossAnimatorAdapter _anim;
    [SerializeField] VisionCone2D _vision;
    [SerializeField] Health _health;
    [SerializeField] LazyMiniBossConfigSO _config;
    [SerializeField] Transform _shootMuzzle;
    [SerializeField] Transform _attack3MuzzleHorns;
    [SerializeField] bool _startFacingLeft = true;

    public LazyMiniBossBrain Brain => _brain;
    public LazyMiniBossMotor2D Motor => _motor;
    public LazyMiniBossAnimatorAdapter Anim => _anim;
    public VisionCone2D Vision => _vision;
    public Health HealthComponent => _health;
    public LazyMiniBossConfigSO Config => _config;

    public Transform Player { get; set; }
    public Vector2 LastSeenPos { get; set; }
    public bool HasSeenPlayer { get; set; }
    public float ForgetTimer { get; set; }
    public bool UseAttack3Next { get; set; }
    public float NextMeleeAttackTime { get; set; }
    public float NextShootAttackTime { get; set; }

    // Zone
    public float ZoneMinX { get; private set; }
    public float ZoneMaxX { get; private set; }
    public bool ZoneReady { get; private set; }

    ProjectilePool _shootPool;
    ProjectilePool _attack3Pool;

    void Awake()
    {
        if (!_brain) _brain = GetComponent<LazyMiniBossBrain>();
        if (!_motor) _motor = GetComponent<LazyMiniBossMotor2D>();
        if (!_anim) _anim = GetComponent<LazyMiniBossAnimatorAdapter>();
        if (!_vision) _vision = GetComponentInChildren<VisionCone2D>();
        if (!_health) _health = GetComponent<Health>();
    }

    void Start()
    {
        if (_startFacingLeft) _motor.Face(-1);

        if (_config != null)
        {
            if (_config.projectilePrefab)
                _shootPool = new ProjectilePool(_config.projectilePrefab, _config.projectilePoolSize);
            if (_config.attack3ProjectilePrefab)
                _attack3Pool = new ProjectilePool(_config.attack3ProjectilePrefab, _config.attack3ProjectilePoolSize);
        }

        RecalcZoneBounds();
    }

    void Update()
    {
        if (_anim.IsInAttack1())
            _anim.SetAttack2(true);

        if (_anim.IsInAttack2())
        {
            _anim.SetAttack1(false);
            _anim.SetAttack2(false);
        }

        if (_anim.IsInAgroMovement())
            _anim.SetAttack3(false);

        Anim.SetXVelocity(Mathf.Abs(Motor.Velocity.x));
    }

    void RecalcZoneBounds()
    {
        ZoneReady = false;
        if (_brain == null) return;
        var path = _brain.PatrolPath;
        if (path == null || path.Count == 0) return;

        float minX = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        for (int i = 0; i < path.Count; i++)
        {
            float x = path.GetPoint(i).x;
            if (x < minX) minX = x;
            if (x > maxX) maxX = x;
        }
        ZoneMinX = minX - 0.05f;
        ZoneMaxX = maxX + 0.05f;
        ZoneReady = true;
    }

    public bool TrySense(out Transform target)
    {
        target = null;
        if (Vision == null) return false;
        return Vision.TryGetClosestTarget(out target);
    }

    public float DistanceToPlayer()
    {
        if (Player == null) return float.MaxValue;
        return Vector2.Distance(transform.position, Player.position);
    }

    public bool IsPlayerInMeleeRange()
    {
        return Config != null && DistanceToPlayer() <= Config.closeRangeThreshold;
    }

    public bool IsPlayerInShootRange()
    {
        return Config != null && DistanceToPlayer() >= Config.shootRangeMin;
    }

    public void FacePlayer()
    {
        if (Player == null || Motor == null) return;
        float dx = Player.position.x - transform.position.x;
        if (Mathf.Abs(dx) > 0.1f)
            Motor.Face(dx > 0 ? 1 : -1);
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
}
