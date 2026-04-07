using UnityEngine;

public class BullBehaviorBridge : MonoBehaviour
{
    [SerializeField] LazyMiniBossBrain _brain;
    [SerializeField] LazyMiniBossMotor2D _motor;
    [SerializeField] LazyMiniBossAnimatorAdapter _anim;
    [SerializeField] VisionCone2D _vision;
    [SerializeField] Health _health;
    [SerializeField] LazyMiniBossConfigSO _config;
    [SerializeField] Transform _projectileSpawnPoint;

    public LazyMiniBossBrain Brain => _brain;
    public LazyMiniBossMotor2D Motor => _motor;
    public LazyMiniBossAnimatorAdapter Anim => _anim;
    public VisionCone2D Vision => _vision;
    public Health HealthComponent => _health;
    public LazyMiniBossConfigSO Config => _config;
    public Transform ProjectileSpawnPoint => _projectileSpawnPoint;

    public Transform Player { get; set; }
    public Vector2 LastSeenPos { get; set; }
    public bool HasSeenPlayer { get; set; }
    public float ForgetTimer { get; set; }
    public bool UseAttack3Next { get; set; }
    public float NextMeleeAttackTime { get; set; }
    public float NextShootAttackTime { get; set; }

    void Awake()
    {
        if (!_brain) _brain = GetComponent<LazyMiniBossBrain>();
        if (!_motor) _motor = GetComponent<LazyMiniBossMotor2D>();
        if (!_anim) _anim = GetComponent<LazyMiniBossAnimatorAdapter>();
        if (!_vision) _vision = GetComponentInChildren<VisionCone2D>();
        if (!_health) _health = GetComponent<Health>();
    }

    void Update()
    {
        if (_anim.IsInAttack1())
        {
            _anim.SetAttack2(true);
        }
        if (_anim.IsInAttack2())
        {
            _anim.SetAttack1(false);
            _anim.SetAttack2(false);
        }
        if (_anim.IsInAgroMovement())
        {
            _anim.SetAttack3(false);
        }
        Anim.SetXVelocity(Mathf.Abs(Motor.Velocity.x));
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

    public void SpawnProjectile()
    {
        if (Config == null || Config.projectilePrefab == null) return;

        Vector3 spawnPos = _projectileSpawnPoint ? _projectileSpawnPoint.position : transform.position;
        GameObject go = Instantiate(Config.projectilePrefab, spawnPos, Quaternion.identity);
        var proj = go.GetComponent<FistProjectile>();

        int dir = Motor.IsFacingRight ? 1 : -1;
        Vector2 direction = new Vector2(dir, 0);

        if (proj)
        {
            proj.Setup(Config.projectileDamage);
            proj.Fire(direction, Config.projectileSpeed);
        }
    }
}
