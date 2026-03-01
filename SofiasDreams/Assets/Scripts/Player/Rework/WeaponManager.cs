using UnityEngine;
using Zenject;

public sealed class WeaponManager : IWeaponManager
{
    readonly SignalBus _bus;

    public WeaponType CurrentWeapon { get; private set; } = WeaponType.Default;

    public WeaponManager(SignalBus bus)
    {
        _bus = bus;
    }

    public void SwitchWeapon()
    {
        CurrentWeapon = CurrentWeapon switch
        {
            WeaponType.Default => WeaponType.Sword,
            WeaponType.Sword   => WeaponType.Dagger,
            WeaponType.Dagger  => WeaponType.Default,
            _                  => WeaponType.Default
        };

        Debug.Log($"[Weapon] Оружие сменилось на: {CurrentWeapon}");
        _bus.Fire(new WeaponSwitched { weapon = CurrentWeapon });
    }
}
