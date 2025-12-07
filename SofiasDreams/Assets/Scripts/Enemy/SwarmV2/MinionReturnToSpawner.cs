using UnityEngine;

public class MinionReturnToSpawner : MonoBehaviour, IReturnToPool
{
    public GuardTargetBinder2D binder;           // чтобы знать, чей мы минион
    SwarmMinionSpawner _spawner;                 // пул роя
    MinionOrbitBrain2D _brain;

    void Reset()
    {
        binder = GetComponent<GuardTargetBinder2D>();
    }

    void Awake()
    {
        if (!binder) binder = GetComponent<GuardTargetBinder2D>();
        _brain = GetComponent<MinionOrbitBrain2D>();
        RefreshSpawner();
    }

    void OnEnable()
    {
        // На случай, если цель биндеру выдали позже
        if (_spawner == null) RefreshSpawner();
    }

    void RefreshSpawner()
    {
        var target = binder ? binder.Target : null;
        _spawner = target ? target.GetComponent<SwarmMinionSpawner>() : null;
    }

    public bool ReturnToPool(GameObject go)
    {
        if (_spawner == null) RefreshSpawner();
        if (_spawner != null && _brain != null)
        {
            // Перед отдачей в пул неплохо бы обнулить скорость
            var rb = GetComponent<Rigidbody2D>();
            if (rb) rb.linearVelocity = Vector2.zero;

            _spawner.Release(_brain);  // вернули миниона его рою
            return true;
        }
        return false;
    }
}
