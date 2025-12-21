using UnityEngine;

public class HurtBox2D : MonoBehaviour, IDamageable
{
    [SerializeField] Health _health;

    public bool IsAlive => _health != null && _health.IsAlive;

    void Reset()
    {
        _health = GetComponentInParent<Health>();
    }

    public void ApplyDamage(int amount, Vector2 hitPoint, Vector2 hitNormal, GameObject source)
    {
        if (_health)
        {
             DamageInfo info = new DamageInfo
            {
                amount = amount,
                hitPoint = hitPoint,
                hitNormal = hitNormal,
                source = source ? source.transform : null
            };
            _health.ApplyDamage(info);
        }
    }
}
