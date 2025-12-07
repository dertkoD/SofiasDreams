using UnityEngine;

public class EnemyDamageReceiver : MonoBehaviour, IDamageable
{
    [Header("Refs")]
    [SerializeField] EnemyFacade _facade;
    [SerializeField] Knockback2D _knockback;
    [SerializeField] EnemyDamageFeedback _feedback;

    [Header("Config")]
    [SerializeField] HitReactionConfig _hitConfig;

    public bool IsAlive
    {
        get
        {
            var h = _facade ? _facade.Health : null;
            return h != null && h.IsAlive;
        }
    }

    void Awake()
    {
        if (!_facade)    _facade   = GetComponentInParent<EnemyFacade>();
        if (!_knockback) _knockback = GetComponentInParent<Knockback2D>();
        if (!_feedback)  _feedback  = GetComponentInParent<EnemyDamageFeedback>();
    }

    public void ApplyDamage(int amount, Vector2 hitPoint, Vector2 hitNormal, GameObject source)
    {
        Debug.Log($"[EnemyDamageReceiver] ApplyDamage start, amount={amount}");
        
        var health = _facade ? _facade.Health : null;
        if (health == null)
        {
            Debug.LogWarning("[EnemyDamageReceiver] No Health on facade");
            return;
        }
        if (!health.IsAlive)
        {
            Debug.Log("[EnemyDamageReceiver] Target already dead");
            return;
        }
        if (health.IsInvincible)
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
        _facade.ApplyDamage(info);
        
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
