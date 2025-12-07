using UnityEngine;

[DisallowMultipleComponent]
public class Hurtbox2D : MonoBehaviour
{
    [SerializeField] MonoBehaviour ownerComponent;

    IDamageable _owner;

    public IDamageable Owner => _owner ??= ResolveOwner();

    void Awake()
    {
        if (_owner == null)
            _owner = ResolveOwner();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (ownerComponent != null && !(ownerComponent is IDamageable))
        {
            Debug.LogWarning($"[Hurtbox2D] Assigned owner on {name} does not implement IDamageable, clearing reference.", this);
            ownerComponent = null;
        }
    }
#endif

    public void Initialize(IDamageable owner)
    {
        _owner = owner;
    }

    IDamageable ResolveOwner()
    {
        if (ownerComponent is IDamageable explicitOwner)
            return explicitOwner;

        return GetComponentInParent<IDamageable>();
    }
}
