using System;
using UnityEngine;
using Zenject;

public class EnemyDamageReceiver : MonoBehaviour, IDamageable
{
    [Header("Refs")]
    [SerializeField] EnemyFacade _facade;
    [Header("Config")]
    [SerializeField] HitReactionConfig _hitConfig;

    IHealth _health;
    IKnockback _knockback;
    IEnemyDamageFeedback _feedback;

    public event Action<DamageInfo> DamageTaken;

    public bool IsAlive
    {
        get
        {
            return _health != null && _health.IsAlive;
        }
    }

    [Inject]
    public void Construct(
        IHealth health,
        [InjectOptional] IKnockback knockback = null,
        [InjectOptional] IEnemyDamageFeedback feedback = null,
        [InjectOptional] EnemyFacade facade = null)
    {
        _health   = health;
        _knockback = knockback;
        _feedback  = feedback;

        if (_facade == null)
            _facade = facade ?? GetComponentInParent<EnemyFacade>();
    }

    public void ApplyDamage(int amount, Vector2 hitPoint, Vector2 hitNormal, GameObject source)
    {
        Debug.Log($"[EnemyDamageReceiver] ApplyDamage start, amount={amount}");
        
        if (_health == null)
        {
            Debug.LogWarning("[EnemyDamageReceiver] No Health on facade");
            return;
        }
        if (!_health.IsAlive)
        {
            Debug.Log("[EnemyDamageReceiver] Target already dead");
            return;
        }
        if (_health.IsInvincible)
        {
            Debug.Log("[EnemyDamageReceiver] Hit ignored: invincible");
            return;
        }

        DamageInfo info = new DamageInfo
        {
            amount      = amount,
            hitPoint    = hitPoint,
            hitNormal   = hitNormal,
            impulse     = hitNormal != Vector2.zero
                ? hitNormal.normalized * _hitConfig.knockbackForce
                : Vector2.zero,
            stunSeconds = _hitConfig.hitStun,
            bypassInvuln = false
        };

        Debug.Log("[EnemyDamageReceiver] -> Health.ApplyDamage");
        if (_facade != null)
            _facade.ApplyDamage(info);
        else
            _health.ApplyDamage(info);

        DamageTaken?.Invoke(info);
        
        if (_feedback != null)
        {
            Vector2 src = (hitPoint != Vector2.zero)
                ? hitPoint
                : (source ? (Vector2)source.transform.position : (Vector2)transform.position);

            Debug.Log("[EnemyDamageReceiver] -> Feedback.OnDamage");
            _feedback.OnDamage(src);
        }
        else
            Debug.LogWarning("[EnemyDamageReceiver] No EnemyDamageFeedback");

        if (_knockback != null)
        {
            Debug.Log($"[EnemyDamageReceiver] -> Knockback.Apply, impulse={info.impulse}, stun={info.stunSeconds}");
            _knockback.Apply(info);
        }
        else
            Debug.LogWarning("[EnemyDamageReceiver] No Knockback2D");
    }
}
