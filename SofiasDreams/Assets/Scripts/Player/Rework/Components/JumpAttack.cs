using System;
using System.Collections;
using UnityEngine;
using Zenject;

public class JumpAttack : MonoBehaviour, IJumpAttack, IInitializable, IDisposable
{
    [Inject] SignalBus _bus;

    PlayerJumpAttackConfig _cfg;
    bool _attacking;
    float _cd;
    Coroutine _cdCo;
    AttackMode _currentMode;

    public bool IsAttacking => _attacking;
    public float CurrentDamage => _cfg ? _cfg.damage : 0f;

    public void Configure(PlayerJumpAttackConfig cfg) => _cfg = cfg;

    public void Initialize()
    {
        _bus.Subscribe<AttackFinished>(OnAttackFinished);
    }

    public void Dispose()
    {
        _bus.Unsubscribe<AttackFinished>(OnAttackFinished);
    }

    public void Request(AttackMode mode)
    {
        if (_cfg == null || _attacking || _cd > 0f) return;

        _attacking = true;
        _currentMode = mode;
        _bus.Fire(new AttackStarted { mode = mode, index = 0 });
    }

    public void Interrupt()
    {
        if (!_attacking) return;

        _attacking = false;
        _bus.Fire(new AttackFinished { mode = _currentMode, index = 0 });
    }

    void OnAttackFinished(AttackFinished e)
    {
        if (!_attacking) return;
        
        // Only respond if it matches our current mode
        if (e.mode != _currentMode) return;

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
