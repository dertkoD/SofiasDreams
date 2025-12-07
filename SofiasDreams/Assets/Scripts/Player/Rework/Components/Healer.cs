using UnityEngine;
using Zenject;

public class Healer : MonoBehaviour, IHealer
{
    SignalBus     _bus;
    IHealth       _health;
    IMobilityGate _gate;

    HealSettings _s;

    int _charges;
    int _kills;
    int _killsPerCharge;
    int _maxCharges;

    bool _healing;

    public bool IsHealing  => _healing;
    
    public int CurrentCharges => _charges;
    public int MaxCharges     => _maxCharges;

    [Inject]
    public void Inject(SignalBus bus, IHealth health, IMobilityGate gate)
    {
        _bus    = bus;
        _health = health;
        _gate   = gate;
    }

    public void Configure(HealSettings s, int maxCharges, int killsPerCharge)
    {
        _s = s;

        _maxCharges     = Mathf.Max(0, maxCharges);
        _killsPerCharge = Mathf.Max(1, killsPerCharge);
        _charges        = _maxCharges;

        _bus.Fire(new HealChargesChanged { current = _charges, max = _maxCharges });
    }

    void OnEnable()  => _bus.Subscribe<EnemyKilled>(OnEnemyKilled);
    void OnDisable() => _bus.Unsubscribe<EnemyKilled>(OnEnemyKilled);

    void OnEnemyKilled(EnemyKilled _)
    {
        if (_charges >= _maxCharges) return;

        _kills++;
        if (_kills >= _killsPerCharge)
        {
            _kills = 0;
            _charges = Mathf.Min(_charges + 1, _maxCharges);
            _bus.Fire(new HealChargesChanged { current = _charges, max = _maxCharges });
        }
    }

    public void StartHeal()
    {
        if (_charges <= 0) return;

        if (!_health.CanHeal()) return;

        if (_healing) return;

        _healing = true;

        _charges = Mathf.Max(0, _charges - 1);
        _bus.Fire(new HealChargesChanged { current = _charges, max = _maxCharges });

        _gate.BlockMovement(MobilityBlockReason.Heal);
        _gate.BlockJump(MobilityBlockReason.Heal);

        _bus.Fire(new HealStarted());
    }

    public void CancelHealing()
    {
        if (!_healing) return;

        _healing = false;

        _gate.UnblockMovement(MobilityBlockReason.Heal);
        _gate.UnblockJump(MobilityBlockReason.Heal);

        _bus.Fire(new HealInterrupted());
    }


    public void PerformHealFromAnimation()
    {
        if (!_healing) return;   

        _healing = false;

        if (_health.CanHeal())
            _health.Heal(_s.amount);

        _gate.UnblockMovement(MobilityBlockReason.Heal);
        _gate.UnblockJump(MobilityBlockReason.Heal);

        _bus.Fire(new HealFinished());
    }
}
