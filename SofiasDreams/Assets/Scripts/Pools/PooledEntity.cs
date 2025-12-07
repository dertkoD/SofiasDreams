using UnityEngine;

public class PooledEntity : MonoBehaviour
{
    private GameObjectPooler _pool;
    public void BindPool(GameObjectPooler pool) => _pool = pool;

    public void ReturnToPool()
    {
        if (_pool) _pool.Return(gameObject); 
        else Destroy(gameObject);
    }
}
