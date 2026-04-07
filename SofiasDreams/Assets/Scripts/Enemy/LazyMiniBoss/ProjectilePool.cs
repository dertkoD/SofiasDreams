using UnityEngine;
using UnityEngine.Pool;

public class ProjectilePool
{
    readonly GameObject _prefab;
    readonly ObjectPool<FistProjectile> _pool;

    public ProjectilePool(GameObject prefab, int defaultSize = 5)
    {
        _prefab = prefab;
        _pool = new ObjectPool<FistProjectile>(
            createFunc: Create,
            actionOnGet: OnGet,
            actionOnRelease: OnRelease,
            actionOnDestroy: OnDestroyPooled,
            defaultCapacity: defaultSize,
            maxSize: defaultSize * 4
        );
    }

    FistProjectile Create()
    {
        var go = Object.Instantiate(_prefab);
        var proj = go.GetComponent<FistProjectile>();
        proj.SetPool(_pool);
        go.SetActive(false);
        return proj;
    }

    void OnGet(FistProjectile proj)
    {
        proj.gameObject.SetActive(true);
    }

    void OnRelease(FistProjectile proj)
    {
        proj.gameObject.SetActive(false);
    }

    void OnDestroyPooled(FistProjectile proj)
    {
        if (proj != null && proj.gameObject != null)
            Object.Destroy(proj.gameObject);
    }

    public FistProjectile Get(Vector3 position, Quaternion rotation)
    {
        var proj = _pool.Get();
        proj.transform.SetPositionAndRotation(position, rotation);
        return proj;
    }
}
