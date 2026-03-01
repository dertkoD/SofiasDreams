using System;
using UnityEngine;
using Zenject;

public class SwordCombat : MonoBehaviour, IInitializable, IDisposable
{
    [SerializeField] Weapon swordWeapon;
    [SerializeField] Rigidbody2D rb;

    SignalBus _bus;
    SwordAttackConfig _cfg;
    AttackSettings _s;
    int _step;
    bool _attacking;
    bool _queued;
    AttackMode? _activeAirMode;
    AttackMode? _activeSuperMode;

    [Inject]
    void Construct(SignalBus bus, [Inject(Optional = true)] SwordAttackConfig cfg = null)
    {
        _bus = bus;
        _cfg = cfg;
    }

    public void Configure(AttackSettings s) => _s = s;

    public bool IsAttacking => _attacking;

    public float CurrentDamage() =>
        _step == 1 ? _s.a1.damage : _step == 2 ? _s.a2.damage : _s.a3.damage;

    public void Initialize()
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        _bus.Subscribe<AttackStarted>(OnAttackStarted);
        _bus.Subscribe<AttackFinished>(OnAttackFinished);
        _bus.Subscribe<EnemyHit>(OnEnemyHit);
    }

    public void Dispose()
    {
        _bus.TryUnsubscribe<AttackStarted>(OnAttackStarted);
        _bus.TryUnsubscribe<AttackFinished>(OnAttackFinished);
        _bus.TryUnsubscribe<EnemyHit>(OnEnemyHit);
    }

    public void RequestAttack()
    {
        if (_attacking) { if (_step < 3) _queued = true; return; }

        _attacking = true;
        _queued = false;
        _step = (_step % 3) + 1;
        ApplyKnockbackForStep();
        _bus.Fire(new AttackStarted { mode = AttackMode.SwordCombo, index = _step });
    }

    public void Interrupt()
    {
        if (_activeSuperMode.HasValue)
        {
            var mode = _activeSuperMode.Value;
            _activeSuperMode = null;
            ResetKnockback();
            _bus.Fire(new AttackFinished { mode = mode, index = 0 });
            return;
        }

        if (!_attacking && _step == 0) return;
        _attacking = false;
        _queued = false;
        _step = 0;
        ResetKnockback();
        _bus.Fire(new AttackFinished { mode = AttackMode.SwordCombo, index = 0 });
    }

    // ───── Charged (super) attack ─────

    public float ChargeTime => _cfg != null ? _cfg.chargeTime : 0.6f;

    public void RequestChargedAttack(bool grounded)
    {
        if (_attacking) Interrupt();

        _activeSuperMode = grounded ? AttackMode.SwordSuper : AttackMode.SwordSuperAir;
        _bus.Fire(new AttackStarted { mode = _activeSuperMode.Value, index = 0 });
    }

    public void FinishFromSwordAnimation()
    {
        if (!_attacking) return;

        if (_queued && _step < 3)
        {
            _queued = false;
            _attacking = false;
            _step++;
            _attacking = true;
            ApplyKnockbackForStep();
            _bus.Fire(new AttackStarted { mode = AttackMode.SwordCombo, index = _step });
            return;
        }

        _attacking = false;
        ResetKnockback();
        _bus.Fire(new AttackFinished { mode = AttackMode.SwordCombo, index = _step });
        _step = 0;
    }

    void ApplyKnockbackForStep()
    {
        if (!swordWeapon) return;
        if (_step < 3)
            swordWeapon.OverrideKnockback(0f);
        else
            swordWeapon.ClearKnockbackOverride();
    }

    void ResetKnockback()
    {
        if (swordWeapon) swordWeapon.ClearKnockbackOverride();
    }

    // ───── Pogo (down-air bounce) ─────

    static bool IsSwordAir(AttackMode m) =>
        m is AttackMode.SwordAirFwd or AttackMode.SwordAirDown or AttackMode.SwordAirUp;

    void OnAttackStarted(AttackStarted s)
    {
        if (IsSwordAir(s.mode))
            _activeAirMode = s.mode;
    }

    void OnAttackFinished(AttackFinished s)
    {
        if (_activeAirMode.HasValue && s.mode == _activeAirMode.Value)
            _activeAirMode = null;
        if (_activeSuperMode.HasValue && s.mode == _activeSuperMode.Value)
            _activeSuperMode = null;
    }

    void OnEnemyHit(EnemyHit e)
    {
        if (_activeAirMode != AttackMode.SwordAirDown) return;
        if (!rb) return;

        float force = _cfg != null ? _cfg.pogoForce : 12f;
        var vel = rb.linearVelocity;
        vel.y = 0f;
        rb.linearVelocity = vel;
        rb.AddForce(Vector2.up * (force * rb.mass), ForceMode2D.Impulse);
    }
}
