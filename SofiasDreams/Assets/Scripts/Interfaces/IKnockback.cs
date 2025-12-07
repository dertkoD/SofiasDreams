using UnityEngine;

public interface IKnockback
{
    bool IsInHitStun { get; }
    void Apply(DamageInfo info);
    void ApplyImpulse(Vector2 impulse, Vector2 hitPoint);
}
