using UnityEngine;
using Zenject;

public class BonfireRespawnableEnemy : MonoBehaviour, IBonfireResettable
{
    [SerializeField] Transform respawnTransformOverride;

    BonfireResetRegistry _registry;

    Vector3 _startPos;
    Quaternion _startRot;
    bool _startActive;

    [Inject]
    public void Construct(BonfireResetRegistry registry) => _registry = registry;

    void Awake()
    {
        var t = respawnTransformOverride != null ? respawnTransformOverride : transform;
        _startPos = t.position;
        _startRot = t.rotation;
        _startActive = gameObject.activeSelf;
    }

    void OnEnable()  => _registry?.Register(this);
    void OnDisable() => _registry?.Unregister(this);

    public void OnBonfireReset()
    {
        var t = respawnTransformOverride != null ? respawnTransformOverride : transform;
        t.SetPositionAndRotation(_startPos, _startRot);

        if (_startActive && !gameObject.activeSelf)
            gameObject.SetActive(true);

        // If your enemy has Health, restore it here.
        // GetComponent<Health>()?.RestoreFull();
    }
}