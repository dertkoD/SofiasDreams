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
    float? _knockbackOverride;

    int Damage => _runtimeConfig ? _runtimeConfig.baseDamage : attackDamage;
    LayerMask TargetLayers => _runtimeConfig ? _runtimeConfig.targetLayers : enemyHurtboxLayers;
    float KnockbackForce => _runtimeConfig ? _runtimeConfig.knockbackForce : -1f;
    float EffectiveKnockback => _knockbackOverride ?? KnockbackForce;
    float BackstabMultiplier => _runtimeConfig ? _runtimeConfig.backstabMultiplier : 1f;

    public void OverrideKnockback(float force) => _knockbackOverride = force;
    public void ClearKnockbackOverride() => _knockbackOverride = null;

    SignalBus _bus;

    // ───── Multi-hit (charged attacks) ─────
    bool _multiHit;
    int _multiHitMax;
    float _multiHitInterval;
    readonly Dictionary<IDamageable, MultiHitEntry> _multiHitData = new();

    struct MultiHitEntry
    {
        public int hits;
        public float nextTime;
        public Collider2D collider;
    }

    public void EnableMultiHit(int maxHits, float duration)
    {
        _multiHit = true;
        _multiHitMax = Mathf.Max(maxHits, 1);
        _multiHitInterval = duration / _multiHitMax;
        _multiHitData.Clear();
    }

    public void DisableMultiHit()
    {
        _multiHit = false;
        _multiHitData.Clear();
    }

    [Inject]
    void Construct(SignalBus bus, [Inject(Optional = true)] PlayerWeaponConfig injectedConfig = null)
    {
        _bus = bus;
        _runtimeConfig = defaultConfig ? defaultConfig : injectedConfig;
    }

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;
    }

    private void OnDisable()
    {
        _hitThisSwing.Clear();
        _multiHitData.Clear();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if ((TargetLayers.value & (1 << other.gameObject.layer)) == 0)
            return;

        var hb = other.GetComponent<Hurtbox2D>();
        var target = hb ? hb.Owner : null;
        if (target == null || !target.IsAlive)
            return;

        if (_multiHit)
        {
            if (!_multiHitData.ContainsKey(target))
            {
                _multiHitData[target] = new MultiHitEntry
                {
                    hits = 0,
                    nextTime = Time.time,
                    collider = other
                };
            }
            TryMultiHit(target, other);
            return;
        }

        if (!_hitThisSwing.Add(target))
            return;

        DealDamage(target, other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!_multiHit) return;
        if ((TargetLayers.value & (1 << other.gameObject.layer)) == 0)
            return;

        var hb = other.GetComponent<Hurtbox2D>();
        var target = hb ? hb.Owner : null;
        if (target == null || !target.IsAlive)
            return;

        TryMultiHit(target, other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if ((TargetLayers.value & (1 << other.gameObject.layer)) == 0)
            return;

        var hb = other.GetComponent<Hurtbox2D>();
        var target = hb ? hb.Owner : null;
        if (target == null) return;

        _hitThisSwing.Remove(target);
    }

    void TryMultiHit(IDamageable target, Collider2D other)
    {
        if (!_multiHitData.TryGetValue(target, out var entry))
            return;
        if (entry.hits >= _multiHitMax) return;
        if (Time.time < entry.nextTime) return;

        entry.hits++;
        entry.nextTime = Time.time + _multiHitInterval;
        entry.collider = other;
        _multiHitData[target] = entry;

        DealDamage(target, other);
    }

    void DealDamage(IDamageable target, Collider2D other)
    {
        Vector2 hitPoint  = other.ClosestPoint(transform.position);
        Vector2 hitNormal = ((Vector2)other.transform.position - (Vector2)transform.position).normalized;

        int dmg = Damage;
        bool backstab = false;
        if (BackstabMultiplier > 1f && target is MonoBehaviour mb)
        {
            Transform enemyT     = mb.transform;
            Transform playerRoot = transform.root;
            float enemyFacing = Mathf.Sign(enemyT.lossyScale.x);
            float playerSide  = Mathf.Sign(playerRoot.position.x - enemyT.position.x);
            if (playerSide != enemyFacing)
            {
                dmg = Mathf.RoundToInt(dmg * BackstabMultiplier);
                backstab = true;
            }
        }

        if (backstab)
            Debug.Log($"[Weapon] Backstab! dmg={dmg} (x{BackstabMultiplier})");

        target.ApplyDamage(dmg, hitPoint, hitNormal, gameObject, EffectiveKnockback);
        _bus.Fire(new EnemyHit { target = target, isBackstab = backstab });
    }
}
