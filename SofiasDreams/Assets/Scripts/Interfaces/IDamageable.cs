using UnityEngine;

public interface IDamageable
{
    bool IsAlive { get; }
    void ApplyDamage(int amount, Vector2 hitPoint, Vector2 hitNormal, GameObject source);
}
