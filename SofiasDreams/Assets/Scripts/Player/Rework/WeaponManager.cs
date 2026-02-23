using UnityEngine;
using Zenject;

public sealed class WeaponManager : IWeaponManager
{
    readonly SignalBus _bus;

    public WeaponType CurrentWeapon { get; private set; } = WeaponType.Sword;

    public WeaponManager(SignalBus bus)
    {
        _bus = bus;
    }

    public void SwitchWeapon()
    {
        CurrentWeapon = CurrentWeapon == WeaponType.Sword
            ? WeaponType.Dagger
            : WeaponType.Sword;

        Debug.Log($"[Weapon] Оружие сменилось на: {CurrentWeapon}");
        _bus.Fire(new WeaponSwitched { weapon = CurrentWeapon });
    }
}
