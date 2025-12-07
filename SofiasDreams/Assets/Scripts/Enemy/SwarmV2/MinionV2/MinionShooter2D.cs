using UnityEngine;

public class MinionShooter2D : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] Transform muzzle;                 // на нём GameObjectPooler
    [SerializeField] bool useMuzzleRight = true;

    [Header("Fire")]
    [SerializeField, Min(0f)] float fireInterval = 0.6f;
    [SerializeField, Min(0f)] float initialFireDelay = 0.5f; // NEW: задержка первого выстрела после спавна

    GameObjectPooler _pool;
    float _nextFireAt;

    void Awake()
    {
        if (!muzzle) muzzle = transform;
        _pool = muzzle.GetComponent<GameObjectPooler>();
        if (!_pool) Debug.LogError("MinionShooter2D: GameObjectPooler not found on muzzle.", this);
    }

    void OnEnable()                                    // NEW: задаём задержку на респавне
    {
        _nextFireAt = Time.time + initialFireDelay;
    }

    public void TryFireAt(Vector2 targetPos)
    {
        if (!_pool || Time.time < _nextFireAt) return;

        Vector2 origin = muzzle.position;
        Vector2 dir = (targetPos - origin).normalized;
        if (dir.sqrMagnitude < 1e-6f) dir = (Vector2)muzzle.right;

        Quaternion rot = Quaternion.FromToRotation(Vector3.right, (Vector3)dir);

        var go = _pool.Get(origin, rot);
        go.transform.SetParent(null, false);
        go.transform.localScale = Vector3.one;

        var bullet = go.GetComponent<MinionBullet>();
        if (bullet) bullet.Fire(dir);

        _nextFireAt = Time.time + fireInterval;
    }
}
