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
        if (_health == null)
        {
            return;
        }
        if (!_health.IsAlive)
        {
            return;
        }
        if (_health.IsInvincible)
        {
            return;
        }

        DamageInfo info = new DamageInfo
        {
            amount      = amount,
            hitPoint    = hitPoint,
            hitNormal   = hitNormal,
            source      = source ? source.transform : null,
            impulse     = hitNormal != Vector2.zero
                ? hitNormal.normalized * _hitConfig.knockbackForce
                : Vector2.zero,
            stunSeconds = _hitConfig.hitStun,
            bypassInvuln = false
        };

        if (_facade != null)
            _facade.ApplyDamage(info);
        else
            _health.ApplyDamage(info);
        
        if (_feedback != null)
        {
            Vector2 src = (hitPoint != Vector2.zero)
                ? hitPoint
                : (source ? (Vector2)source.transform.position : (Vector2)transform.position);

            _feedback.OnDamage(src);
        }

        if (_knockback != null)
        {
            _knockback.Apply(info);
        }
    }
}
