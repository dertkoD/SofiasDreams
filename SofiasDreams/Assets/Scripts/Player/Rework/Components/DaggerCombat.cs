using System;
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
    bool _superQueued;
    bool _step1Hit;
    bool _step2Hit;
    Coroutine _floatCo;

    public bool IsAttacking => _attacking;
    public bool CanSuperAttack => _step == 2;

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
        _bus.Subscribe<EnemyHit>(OnEnemyHit);
    }

    public void Dispose()
    {
        _bus.TryUnsubscribe<EnemyHit>(OnEnemyHit);
    }

    public void RequestAttack()
    {
        if (_attacking) { if (_step < 2) _queued = true; return; }

        _attacking = true;
        _queued = false;
        _superQueued = false;
        _step = (_step % 2) + 1;
        _bus.Fire(new AttackStarted { mode = AttackMode.DaggerCombo, index = _step });
    }

    public bool TryRequestSuperAttack()
    {
        if (!CanSuperAttack) return false;

        if (_attacking)
        {
            _superQueued = true;
            return true;
        }

        StartSuper();
        return true;
    }

    public void Interrupt()
    {
        if (!_attacking && _step == 0) return;

        if (_floatCo != null) { StopCoroutine(_floatCo); _floatCo = null; }

        var mode = _step == 3 ? AttackMode.DaggerSuper : AttackMode.DaggerCombo;
        _attacking = false;
        _queued = false;
        _superQueued = false;
        _step = 0;
        _step1Hit = false;
        _step2Hit = false;
        _bus.Fire(new AttackFinished { mode = mode, index = 0 });
    }

    public void DaggerFinishFromAnimation()
    {
        if (!_attacking) return;

        if (_superQueued && _step == 2)
        {
            _superQueued = false;
            _queued = false;
            _attacking = false;
            StartSuper();
            return;
        }

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
        _step1Hit = false;
        _step2Hit = false;
    }

    void StartSuper()
    {
        _attacking = true;
        _step = 3;
        _bus.Fire(new AttackStarted { mode = AttackMode.DaggerSuper, index = 3 });
    }

    void OnEnemyHit(EnemyHit e)
    {
        if (!_attacking) return;

        switch (_step)
        {
            case 1: _step1Hit = true; break;
            case 2: _step2Hit = true; break;
            case 3:
                if (_step1Hit && _step2Hit)
                    ApplyFloat();
                break;
        }
    }

    void ApplyFloat()
    {
        if (!rb || _cfg == null) return;

        var v = rb.linearVelocity;
        v.y = _cfg.superLaunchForce;
        rb.linearVelocity = v;

        if (_floatCo != null) StopCoroutine(_floatCo);
        _floatCo = StartCoroutine(FloatRoutine());
    }

    IEnumerator FloatRoutine()
    {
        float origGravity = rb.gravityScale;
        rb.gravityScale = _cfg != null ? _cfg.floatGravityScale : 0.1f;

        float duration = _cfg != null ? _cfg.floatDuration : 0.6f;
        yield return new WaitForSeconds(duration);

        rb.gravityScale = origGravity;
        _floatCo = null;
    }
}
