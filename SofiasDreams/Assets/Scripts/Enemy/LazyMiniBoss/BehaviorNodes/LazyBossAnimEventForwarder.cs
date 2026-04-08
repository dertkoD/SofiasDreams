using UnityEngine;

/// <summary>
/// Sits on the child GameObject that has the Animator.
/// Forwards animation events to LazyMiniBossGraphBridge on the root.
/// Auto-added by LazyMiniBossGraphBridge.EnsureAnimEventForwarder().
/// </summary>
public class LazyBossAnimEventForwarder : MonoBehaviour
{
    LazyMiniBossGraphBridge _bridge;

    public void SetBridge(LazyMiniBossGraphBridge bridge) => _bridge = bridge;

    void Awake()
    {
        if (_bridge == null)
            _bridge = GetComponentInParent<LazyMiniBossGraphBridge>();
    }

    public void AnimationEvent_SpawnShootProjectile()
    {
        if (_bridge) _bridge.SpawnShootProjectile();
    }

    public void AnimationEvent_SpawnAttack3Projectile()
    {
        if (_bridge) _bridge.SpawnAttack3Projectile();
    }

    public void AnimationEvent_SpawnProjectile()
    {
        if (_bridge) _bridge.AnimationEvent_SpawnProjectile();
    }
}
