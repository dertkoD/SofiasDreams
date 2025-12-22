using UnityEngine;
using Zenject;

public class MinionShooter2D : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] Transform muzzle;
    
    // We assume standard Instantiate if no pooler found, or use pooler.
    // Given the previous code used GameObjectPooler, we try to use it if present.
    GameObjectPooler _pool;
    
    MinionConfig _config;
    float _nextFireAt;

    [Inject]
    public void Construct(MinionConfig config)
    {
        _config = config;
    }

    void Awake()
    {
        if (!muzzle) muzzle = transform;
        _pool = muzzle.GetComponent<GameObjectPooler>();
        // If no pooler, we will instantiate from prefab in config
    }

    void OnEnable()
    {
        if (_config)
            _nextFireAt = Time.time + _config.initialFireDelay;
    }

    public void TryFireAt(Vector2 targetPos)
    {
        if (_config == null || Time.time < _nextFireAt) return;

        Vector2 origin = muzzle.position;
        Vector2 dir = (targetPos - origin).normalized;
        if (dir.sqrMagnitude < 1e-6f) dir = (Vector2)muzzle.right;

        Quaternion rot = Quaternion.FromToRotation(Vector3.right, (Vector3)dir);

        GameObject go = null;
        if (_pool)
        {
            go = _pool.Get(origin, rot);
        }
        else if (_config.bulletPrefab)
        {
            go = Instantiate(_config.bulletPrefab, origin, rot);
        }

        if (go)
        {
            var bullet = go.GetComponent<MinionBullet>();
            if (bullet) bullet.Fire(dir);
        }

        _nextFireAt = Time.time + _config.fireCooldown;
    }
}
