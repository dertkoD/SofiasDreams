public interface IPlayerAnimator
{
    void SetMoveSpeed(float speed01);
    void SetGrounded(bool grounded);

    void PlayAttack(int index);
    void PlayUpAttack();
    void PlayAirForwardAttack();
    void PlayAirDownAttack();
    void PlayAirUpAttack();

    void PlayDaggerAttack(int index);
    void PlayDaggerSuperAttack();
    void PlayDaggerFlyAttackUp();
    void PlayDaggerFlyAttackDown();

    void PlayChangeWeapon(System.Action onComplete = null);
    void PlayDaggerParry();

    void PlayHealStart();
    void PlayHealEnd(bool interrupted, System.Action onComplete = null);
    void PlayHurt();
    void PlayDeath();
}
