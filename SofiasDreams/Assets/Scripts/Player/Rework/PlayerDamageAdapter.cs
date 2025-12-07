using UnityEngine;
using Zenject;

public class PlayerDamageAdapter : MonoBehaviour, IDamageable
{
    [SerializeField] float defaultKnockbackForce = 6f;

    IPlayerCommands _commands;
    IHealth _health;
    HitReactionConfig _hitReaction;

    [Inject]
    void Construct(
        IPlayerCommands commands,
        IHealth health,
        [Inject(Optional = true)] HitReactionConfig hitReaction = null)
    {
        _commands = commands;
        _health = health;
        _hitReaction = hitReaction;
    }

    public bool IsAlive => _health != null && _health.IsAlive;

    public void ApplyDamage(int amount, Vector2 hitPoint, Vector2 hitNormal, GameObject source)
    {
        if (_commands == null || !IsAlive)
            return;

        float knockbackForce = _hitReaction != null ? _hitReaction.knockbackForce : defaultKnockbackForce;
        Vector2 impulse = hitNormal == Vector2.zero
            ? Vector2.zero
            : -hitNormal.normalized * knockbackForce;

        var info = new DamageInfo {
            amount      = amount,
            type        = DamageType.Melee,
            source      = source ? source.transform : null,
            hitPoint    = hitPoint,
            hitNormal   = hitNormal,
            impulse     = impulse,
            stunSeconds = 0f,
            bypassInvuln = false,
            isCritical   = false
        };

        _commands.ApplyDamage(info);
    }
}
