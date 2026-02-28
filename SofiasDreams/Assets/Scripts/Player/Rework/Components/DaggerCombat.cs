using System;
using System.Collections;
using UnityEngine;
using Zenject;

public class DaggerCombat : MonoBehaviour, IInitializable, IDisposable
{
    [SerializeField] Rigidbody2D rb;

    SignalBus _bus;
    DaggerAttackConfig _cfg;
    DaggerMomentum _momentum;

    int _step;
    bool _attacking;
    bool _queued;
    Coroutine _floatCo;
    float _origGravity;
    bool _gravityOverridden;
    bool _parrying;
    float _chargedCooldownTimer;

    public bool IsAttacking => _attacking;
    public bool IsParrying => _parrying;

    public float CurrentDamage =>
        _step == 1 ? (_cfg ? _cfg.damage1 : 8f) :
        _step == 2 ? (_cfg ? _cfg.damage2 : 8f) :
        (_cfg ? _cfg.superDamage : 25f);

    [Inject]
    void Construct(SignalBus bus,
        [Inject(Optional = true)] DaggerAttackConfig cfg = null,
        [Inject(Optional = true)] DaggerMomentum momentum = null)
    {
        _bus = bus;
        _cfg = cfg;
        _momentum = momentum;
    }

    public void Configure(DaggerAttackConfig cfg) => _cfg = cfg;

    public void Initialize()
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        _origGravity = rb ? rb.gravityScale : 1f;
        _bus.Subscribe<AttackStarted>(OnAttackStarted);
        _bus.Subscribe<AttackFinished>(OnAttackFinished);
    }

    public void Dispose()
    {
        _bus.TryUnsubscribe<AttackStarted>(OnAttackStarted);
        _bus.TryUnsubscribe<AttackFinished>(OnAttackFinished);
    }

    // ───── Combo (2 hits) ─────

    public void RequestAttack()
    {
        if (_attacking) { if (_step < 2) _queued = true; return; }

        _attacking = true;
        _queued = false;
        _step = (_step % 2) + 1;
        _bus.Fire(new AttackStarted { mode = AttackMode.DaggerCombo, index = _step });
    }

    public void DaggerFinishFromAnimation()
    {
        if (!_attacking) return;

        if (_queued && _step < 2)
        {
            _queued = false;
            _attacking = false;
            _step++;
            _attacking = true;
            _bus.Fire(new AttackStarted { mode = AttackMode.DaggerCombo, index = _step });
            return;
        }

        var mode = _step == 3 ? AttackMode.DaggerSuper : AttackMode.DaggerCombo;
        _attacking = false;
        _bus.Fire(new AttackFinished { mode = mode, index = _step });
        _step = 0;
        RestoreGravity();
    }

    // ───── Charged attack (independent) ─────

    public bool IsChargedReady => _chargedCooldownTimer <= 0f;

    void Update()
    {
        if (_chargedCooldownTimer > 0f)
            _chargedCooldownTimer -= Time.deltaTime;
    }

    public void RequestChargedAttack()
    {
        if (_chargedCooldownTimer > 0f) return;
        if (_attacking) Interrupt();

        _attacking = true;
        _step = 3;
        _chargedCooldownTimer = _cfg ? _cfg.chargedCooldown : 1.5f;
        StopFloatCoroutine();
        _floatCo = StartCoroutine(LaunchPlayerRoutine());
        _bus.Fire(new AttackStarted { mode = AttackMode.DaggerSuper, index = 3 });
        Debug.Log($"[DaggerCombat] ChargedAttack fired! force={(_cfg ? _cfg.playerLaunchForce : 0)}");
    }

    public void Interrupt()
    {
        if (!_attacking && _step == 0) return;

        StopFloatCoroutine();
        RestoreGravity();

        var mode = _step == 3 ? AttackMode.DaggerSuper : AttackMode.DaggerCombo;
        _attacking = false;
        _queued = false;
        _step = 0;
        _bus.Fire(new AttackFinished { mode = mode, index = 0 });
    }

    // ───── Player launch on charged attack ─────

    IEnumerator LaunchPlayerRoutine()
    {
        if (!rb || _cfg == null) yield break;

        if (!_gravityOverridden)
        {
            _origGravity = rb.gravityScale;
            _gravityOverridden = true;
        }
        rb.gravityScale = _cfg.floatGravityScale;

        yield return new WaitForFixedUpdate();

        float force = _cfg.playerLaunchForce;
        rb.linearVelocity = new Vector2(0f, 0f);
        rb.AddForce(Vector2.up * (force * rb.mass), ForceMode2D.Impulse);

        Debug.Log($"[DaggerCombat] Launch applied! vel={rb.linearVelocity}, gravity={rb.gravityScale}");

        yield return new WaitForSeconds(_cfg.floatGravityDuration);

        RestoreGravity();
        _floatCo = null;
    }

    // ───── Parry ─────

    public void RequestParry()
    {
        _parrying = true;
    }

    public void ParryFinishFromAnimation()
    {
        _parrying = false;
    }

    public bool TryExecuteParry(Transform attacker)
    {
        if (attacker == null) return false;

        _parrying = false;

        TeleportBehind(attacker);
        StunEnemy(attacker);
        _momentum?.OnParrySuccess();

        Debug.Log("[DaggerCombat] Parry successful!");
        return true;
    }

    void TeleportBehind(Transform enemy)
    {
        if (!rb) return;

        float enemyFacing = Mathf.Sign(enemy.lossyScale.x);
        float offset = _cfg != null ? _cfg.parryTeleportOffset : 1.5f;
        float behindX = enemy.position.x - enemyFacing * offset;

        rb.position = new Vector2(behindX, rb.position.y);

        var mover = GetComponent<Mover2D>();
        if (mover) mover.ForceFacing((int)enemyFacing);
    }

    void StunEnemy(Transform enemy)
    {
        var kb = enemy.GetComponentInChildren<Knockback2D>();
        if (kb == null) return;

        float stunDur = _cfg != null ? _cfg.parryStunDuration : 1f;
        var info = new DamageInfo
        {
            amount = 0,
            impulse = Vector2.zero,
            stunSeconds = stunDur
        };
        kb.Apply(info);
    }

    // ───── Air attack hover ─────

    static bool IsDaggerAirMode(AttackMode m) =>
        m is AttackMode.DaggerFlyUp or AttackMode.DaggerFlyDown;

    void OnAttackStarted(AttackStarted s)
    {
        if (!IsDaggerAirMode(s.mode) || !rb) return;

        if (!_gravityOverridden)
        {
            _origGravity = rb.gravityScale;
            _gravityOverridden = true;
        }

        rb.gravityScale = _cfg != null ? _cfg.airHoverGravityScale : 0f;
        var v = rb.linearVelocity;
        v.y = 0f;
        rb.linearVelocity = v;
    }

    void OnAttackFinished(AttackFinished s)
    {
        if (!IsDaggerAirMode(s.mode)) return;
        RestoreGravity();
    }

    void RestoreGravity()
    {
        if (!_gravityOverridden || !rb) return;
        rb.gravityScale = _origGravity;
        _gravityOverridden = false;
    }

    void StopFloatCoroutine()
    {
        if (_floatCo != null)
        {
            StopCoroutine(_floatCo);
            _floatCo = null;
        }
    }
}
