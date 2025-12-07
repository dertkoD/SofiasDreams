using System;
using System.Collections;
using UnityEngine;
using Zenject;

public class UpAttack : MonoBehaviour, IUpAttack, IInitializable, IDisposable
{
    [Inject] SignalBus _bus;

    PlayerUpAttackConfig _cfg;
    bool _attacking;
    float _cd;
    Coroutine _cdCo;

    public bool  IsAttacking   => _attacking;
    public float CurrentDamage => _cfg ? _cfg.damage : 0f;

    public void Configure(PlayerUpAttackConfig cfg) => _cfg = cfg;

    public void Initialize()
    {
        _bus.Subscribe<AttackFinished>(OnAttackFinished);
    }

    public void Dispose()
    {
        _bus.Unsubscribe<AttackFinished>(OnAttackFinished);
    }

    public void Request()
    {
        if (_cfg == null || _attacking || _cd > 0f) return;

        _attacking = true;
        _bus.Fire(new AttackStarted { mode = AttackMode.Up, index = 0 });
    }

    public void Interrupt()
    {
        if (!_attacking) return;

        _attacking = false;
        _bus.Fire(new AttackFinished { mode = AttackMode.Up, index = 0 });
    }

    void OnAttackFinished(AttackFinished e)
    {
        if (e.mode != AttackMode.Up) return;
        if (!_attacking && _cfg == null) return;

        _attacking = false;
        if (_cfg != null && _cfg.cooldown > 0f)
        {
            if (_cdCo != null) StopCoroutine(_cdCo);
            _cdCo = StartCoroutine(Cooldown());
        }
    }

    IEnumerator Cooldown()
    {
        _cd = _cfg.cooldown;
        while (_cd > 0f) { _cd -= Time.deltaTime; yield return null; }
        _cd = 0f;
    }
}
