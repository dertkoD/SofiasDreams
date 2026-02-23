public interface IWeaponManager
{
    WeaponType CurrentWeapon { get; }
    void SwitchWeapon();
}
