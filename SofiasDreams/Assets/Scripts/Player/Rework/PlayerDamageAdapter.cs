using UnityEngine;
using Zenject;

public class PlayerDamageAdapter : MonoBehaviour, IDamageable
{
    [Inject] IPlayerCommands _commands;

    [SerializeField] float defaultKnockbackForce = 6f;

    public bool IsAlive => true;

    public void ApplyDamage(int amount, Vector2 hitPoint, Vector2 hitNormal, GameObject source)
    {
        Vector2 impulse = -hitNormal.normalized * defaultKnockbackForce;

        var info = new DamageInfo {
            amount    = amount,
            type      = DamageType.Melee,
            source    = source ? source.transform : null,
            hitPoint  = hitPoint,
            hitNormal = hitNormal,
            impulse   = impulse,
            stunSeconds = 0f,
            bypassInvuln = false,
            isCritical   = false
        };

        _commands.ApplyDamage(info);
    }
}
