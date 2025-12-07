using System;
using UnityEngine;
using Zenject;

public class Health : MonoBehaviour, IHealth
{
    HealthSettings _s;
    SignalBus _bus;

    int   _hp;
    float _inv;

    public event Action OnHealthChanged;
    
    public void Configure(HealthSettings s)
    {
        _s  = s;
        _hp = s.maxHP;
        OnHealthChanged?.Invoke();        
    }

    public void Inject(SignalBus bus) => _bus = bus;
    
    public int  CurrentHP    => _hp;
    public int  MaxHP        => _s.maxHP;
    public bool IsAlive      => _hp > 0;
    public bool IsInvincible => _inv > 0f;

    public bool CanHeal() => IsAlive && _hp < _s.maxHP;

    public void Heal(int amount)
    {
        if (!CanHeal()) return;

        int old = _hp;
        _hp = Mathf.Min(_hp + Mathf.Max(0, amount), _s.maxHP);

        if (_hp != old)
            OnHealthChanged?.Invoke();
    }

    public void TakeDamage(int amount)
    {
        if (!IsAlive) return;

        int dmg = Mathf.Max(0, amount);
        if (dmg == 0) return;

        int old = _hp;
        _hp = Mathf.Max(0, _hp - dmg);

        if (_hp != old)
            OnHealthChanged?.Invoke();

        _bus?.Fire(new TookDamage { amount = dmg });

        if (_hp == 0)
        {
            _bus?.Fire(new Died());
        }
        else
        {
            _inv = _s.invulnTime;
        }
    }

    public void ApplyDamage(DamageInfo info)
    {
        if (!IsAlive) return;
        if (IsInvincible && !info.bypassInvuln)
        {
            Debug.Log("[Health] Hit ignored: invincible");
            return;
        }

        Debug.Log($"[Health] ApplyDamage: {info.amount}, hp before = {_hp}");
        TakeDamage(info.amount);
    }

    void Update()
    {
        if (_inv > 0f)
            _inv -= Time.deltaTime;
    }
}
