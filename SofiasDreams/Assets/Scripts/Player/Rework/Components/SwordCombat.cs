using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class SwordCombat : MonoBehaviour, IInitializable, IDisposable
{
    [SerializeField] Weapon swordWeapon;
    [SerializeField] Rigidbody2D rb;

    SignalBus _bus;
    SwordAttackConfig _cfg;
    IInputService _input;
    IWeaponManager _weaponManager;
    AttackSettings _s;
    int _step;
    bool _attacking;
    bool _queued;
    AttackMode? _activeAirMode;
    AttackMode? _activeSuperMode;
    float _chargeProgress;
    bool _wasHeld;
    bool _dashAttackBuffered;
    bool _isDashing;

    // ───── Multi-hit (charged attack extra ticks) ─────
    bool _superActive;
    readonly Dictionary<IDamageable, Coroutine> _extraHitRoutines = new();

    [Inject]
    void Construct(SignalBus bus,
        [Inject(Optional = true)] SwordAttackConfig cfg = null,
        [Inject(Optional = true)] IInputService input = null,
        [Inject(Optional = true)] IWeaponManager weaponManager = null)
    {
        _bus = bus;
        _cfg = cfg;
        _input = input;
        _weaponManager = weaponManager;
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
        _bus.Subscribe<DashStarted>(OnDashStarted);
        _bus.Subscribe<DashFinished>(OnDashFinished);
    }

    public void Dispose()
    {
        _bus.TryUnsubscribe<AttackStarted>(OnAttackStarted);
        _bus.TryUnsubscribe<AttackFinished>(OnAttackFinished);
        _bus.TryUnsubscribe<EnemyHit>(OnEnemyHit);
        _bus.TryUnsubscribe<DashStarted>(OnDashStarted);
        _bus.TryUnsubscribe<DashFinished>(OnDashFinished);
    }

    void Update()
    {
        if (_input == null || _weaponManager == null)
        {
            Debug.LogWarning($"[SwordCombat] Update skip: _input={_input != null}, _weaponManager={_weaponManager != null}");
            return;
        }
        if (_weaponManager.CurrentWeapon != WeaponType.Sword) return;

        if (_isDashing)
        {
            bool pressed = _input.AttackPressed();
            bool held2 = _input.AttackHeld();
            if (pressed)
            {
                _dashAttackBuffered = true;
                Debug.Log($"[SwordCombat] BUFFERED via Update! AttackPressed={pressed}, AttackHeld={held2}");
            }
        }

        bool held = _input.AttackHeld();

        if (held)
        {
            _chargeProgress = Mathf.Clamp01(_chargeProgress + Time.deltaTime / ChargeTime);
            _bus.Fire(new SwordChargeChanged { progress = _chargeProgress });
            _wasHeld = true;
        }
        else if (_wasHeld)
        {
            _chargeProgress = 0f;
            _bus.Fire(new SwordChargeChanged { progress = 0f });
            _wasHeld = false;
        }
    }

    // ───── Dash attack ─────

    void OnDashStarted(DashStarted _)
    {
        _isDashing = true;
        _dashAttackBuffered = false;
        Debug.Log("[SwordCombat] OnDashStarted → _isDashing=true, buffer cleared");
    }

    void OnDashFinished(DashFinished _)
    {
        Debug.Log($"[SwordCombat] OnDashFinished → _dashAttackBuffered={_dashAttackBuffered}, weapon={_weaponManager?.CurrentWeapon}");
        _isDashing = false;
        if (!_dashAttackBuffered) return;
        if (_weaponManager == null || _weaponManager.CurrentWeapon != WeaponType.Sword) return;

        _dashAttackBuffered = false;
        Debug.Log("[SwordCombat] FIRING SwordDashAttack from OnDashFinished!");
        _bus.Fire(new AttackStarted { mode = AttackMode.SwordDashAttack, index = 0 });
    }

    public void BufferDashAttack()
    {
        _dashAttackBuffered = true;
        Debug.Log("[SwordCombat] BufferDashAttack called from PlayerStateMachine");
    }

    public void OnDashEnd()
    {
        if (!_dashAttackBuffered) return;
        _dashAttackBuffered = false;
        _bus.Fire(new AttackStarted { mode = AttackMode.SwordDashAttack, index = 0 });
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
            StopAllExtraHits();
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
        _superActive = true;

        _bus.Fire(new AttackStarted { mode = _activeSuperMode.Value, index = 0 });
    }

    public void FinishFromSwordAnimation()
    {
        if (!_attacking) return;
        if (!_queued || _step >= 3) return;

        AdvanceComboStep();
    }

    public void FinishSwordStep()
    {
        if (!_attacking) return;

        if (_queued && _step < 3)
        {
            AdvanceComboStep();
            return;
        }

        _attacking = false;
        ResetKnockback();
        _bus.Fire(new AttackFinished { mode = AttackMode.SwordCombo, index = _step });
        _step = 0;
    }

    void AdvanceComboStep()
    {
        _queued = false;
        _attacking = false;
        _step++;
        _attacking = true;
        ApplyKnockbackForStep();
        _bus.Fire(new AttackStarted { mode = AttackMode.SwordCombo, index = _step });
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
        {
            _activeSuperMode = null;
            StopAllExtraHits();
        }
    }

    void StopAllExtraHits()
    {
        _superActive = false;
        foreach (var co in _extraHitRoutines.Values)
            if (co != null) StopCoroutine(co);
        _extraHitRoutines.Clear();
    }

    IEnumerator ExtraHitsRoutine(IDamageable target, Collider2D hurtboxCol)
    {
        int extraHits = (_cfg != null ? _cfg.chargedMaxHits : 3) - 1;
        float interval = (_cfg != null ? _cfg.chargedHitDuration : 4f) / (_cfg != null ? _cfg.chargedMaxHits : 3);

        for (int i = 0; i < extraHits; i++)
        {
            yield return new WaitForSeconds(interval);

            if (!_superActive) yield break;
            if (!target.IsAlive) yield break;

            swordWeapon.DealDamage(target, hurtboxCol, bypassInvuln: true);
        }

        _extraHitRoutines.Remove(target);
    }

    void OnEnemyHit(EnemyHit e)
    {
        if (_superActive && e.target != null && !_extraHitRoutines.ContainsKey(e.target))
        {
            var hurtboxCol = FindHurtboxCollider(e.target);
            if (hurtboxCol)
                _extraHitRoutines[e.target] = StartCoroutine(ExtraHitsRoutine(e.target, hurtboxCol));
        }

        if (_activeAirMode != AttackMode.SwordAirDown) return;
        if (!rb) return;

        float force = _cfg != null ? _cfg.pogoForce : 12f;
        var vel = rb.linearVelocity;
        vel.y = 0f;
        rb.linearVelocity = vel;
        rb.AddForce(Vector2.up * (force * rb.mass), ForceMode2D.Impulse);
    }

    static Collider2D FindHurtboxCollider(IDamageable target)
    {
        if (target is MonoBehaviour mb)
        {
            var hb = mb.GetComponentInChildren<Hurtbox2D>();
            if (hb) return hb.GetComponent<Collider2D>();
        }
        return null;
    }
}
