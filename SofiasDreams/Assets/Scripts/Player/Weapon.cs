using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class Weapon : MonoBehaviour
{
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private LayerMask enemyHurtboxLayers;
    [SerializeField] private PlayerWeaponConfig defaultConfig;

    PlayerWeaponConfig _runtimeConfig;
    readonly HashSet<IDamageable> _hitThisSwing = new();

    int Damage => _runtimeConfig ? _runtimeConfig.baseDamage : attackDamage;
    LayerMask TargetLayers => _runtimeConfig ? _runtimeConfig.targetLayers : enemyHurtboxLayers;

    SignalBus _bus;

    [Inject]
    void Construct(SignalBus bus, [Inject(Optional = true)] PlayerWeaponConfig injectedConfig = null)
    {
        _bus = bus;
        _runtimeConfig = injectedConfig ? injectedConfig : defaultConfig;
    }

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;
    }

    private void OnDisable()
    {
        _hitThisSwing.Clear();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if ((TargetLayers.value & (1 << other.gameObject.layer)) == 0)
            return;

        var hb = other.GetComponent<Hurtbox2D>();
        var target = hb ? hb.Owner : null;
        if (target == null || !target.IsAlive)
            return;

        if (!_hitThisSwing.Add(target))
            return;

        Vector2 hitPoint  = other.ClosestPoint(transform.position);
        Vector2 hitNormal = ((Vector2)other.transform.position - (Vector2)transform.position).normalized;

        target.ApplyDamage(Damage, hitPoint, hitNormal, gameObject);
        _bus.Fire(new EnemyHit { target = target });
    }
}
