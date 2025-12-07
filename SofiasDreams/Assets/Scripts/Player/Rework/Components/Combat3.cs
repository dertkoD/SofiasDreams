using UnityEngine;
using Zenject;

public class Combat3 : MonoBehaviour, ICombat
{
    AttackSettings _s; SignalBus _bus;
    int _step; bool _attacking; bool _queued;

    [Inject] public void Inject(SignalBus bus) => _bus = bus;
    public void Configure(AttackSettings s) => _s = s;

    public bool IsAttacking => _attacking;
    public float CurrentDamage() =>
        _step == 1 ? _s.a1.damage : _step == 2 ? _s.a2.damage : _s.a3.damage;

    public void RequestAttack()
    {
        if (_attacking) { if (_step < 3) _queued = true; return; }

        _attacking = true;
        _queued = false;
        _step = (_step % 3) + 1;          // 1..3
        _bus.Fire(new AttackStarted { index = _step });
    }

    public void Interrupt()
    {
        if (!_attacking && _step == 0) return;
        _attacking = false; _queued = false; _step = 0;
        _bus.Fire(new AttackFinished { index = 0 });
    }

    public void FinishFromAnimation()
    {
        if (!_attacking) return;

        if (_queued && _step < 3)
        {
            _queued = false;
            _attacking = false;
            _step++;                      
            _attacking = true;
            _bus.Fire(new AttackStarted { index = _step });
            return;
        }

        _attacking = false;
        _bus.Fire(new AttackFinished { index = _step });
        _step = 0;
    }
}
