using System.Collections;
using UnityEngine;
using Zenject;

public class DaggerCombat : MonoBehaviour, IInitializable, IDisposable
{
    [SerializeField] Rigidbody2D rb;

    SignalBus _bus;
    DaggerAttackConfig _cfg;

    int _step;
    bool _attacking;
    bool _queued;
    Coroutine _floatCo;
    float _origGravity;
    bool _gravityOverridden;

    public bool IsAttacking => _attacking;

    public float CurrentDamage =>
        _step == 1 ? (_cfg ? _cfg.damage1 : 8f) :
        _step == 2 ? (_cfg ? _cfg.damage2 : 8f) :
        (_cfg ? _cfg.superDamage : 25f);

    [Inject]
    void Construct(SignalBus bus, [Inject(Optional = true)] DaggerAttackConfig cfg = null)
    {
        _bus = bus;
        _cfg = cfg;
    }

    public void Configure(DaggerAttackConfig cfg) => _cfg = cfg;

    public void Initialize()
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        _origGravity = rb ? rb.gravityScale : 1f;
        _bus.Subscribe<EnemyHit>(OnEnemyHit);
        _bus.Subscribe<AttackStarted>(OnAttackStarted);
        _bus.Subscribe<AttackFinished>(OnAttackFinished);
    }

    public void Dispose()
    {
        _bus.TryUnsubscribe<EnemyHit>(OnEnemyHit);
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

    public void RequestChargedAttack()
    {
        if (_attacking) Interrupt();

        _attacking = true;
        _step = 3;
        LaunchPlayer();
        _bus.Fire(new AttackStarted { mode = AttackMode.DaggerSuper, index = 3 });
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

    // ───── Hit → launch enemy + player float ─────

    void OnEnemyHit(EnemyHit e)
    {
        if (!_attacking || _step != 3) return;

        LaunchEnemy(e.target);
    }

    void LaunchPlayer()
    {
        if (!rb || _cfg == null) return;

        var v = rb.linearVelocity;
        v.y = _cfg.playerLaunchForce;
        rb.linearVelocity = v;

        StopFloatCoroutine();
        _floatCo = StartCoroutine(FloatGravityRoutine());
    }

    void LaunchEnemy(IDamageable target)
    {
        if (_cfg == null) return;
        if (target is not MonoBehaviour mb) return;

        var enemyRb = mb.GetComponentInParent<Rigidbody2D>();
        if (enemyRb)
            enemyRb.AddForce(Vector2.up * _cfg.enemyLaunchForce, ForceMode2D.Impulse);
    }

    IEnumerator FloatGravityRoutine()
    {
        if (!_gravityOverridden)
        {
            _origGravity = rb.gravityScale;
            _gravityOverridden = true;
        }

        rb.gravityScale = _cfg != null ? _cfg.floatGravityScale : 0.1f;

        float duration = _cfg != null ? _cfg.floatGravityDuration : 0.3f;
        yield return new WaitForSeconds(duration);

        RestoreGravity();
        _floatCo = null;
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
