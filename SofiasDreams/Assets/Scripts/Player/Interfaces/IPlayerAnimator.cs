public interface IPlayerAnimator
{
    void SetMoveSpeed(float speed01);
    void SetGrounded(bool grounded);

    void PlayAttack(int index);
    void PlayUpAttack();
    void PlayAirForwardAttack();
    void PlayAirDownAttack();
    void PlayAirUpAttack();

    void PlaySwordAttack(int index);
    void PlaySwordDashAttack();
    void PlaySwordSuperAttack();
    void PlaySwordSuperAirAttack();
    void PlaySwordAirForwardAttack();
    void PlaySwordAirDownAttack();
    void PlaySwordAirUpAttack();

    void PlayDaggerAttack(int index);
    void PlayDaggerSuperAttack();
    void PlayDaggerFlyAttackUp();
    void PlayDaggerFlyAttackDown();

    void PlayChangeWeapon(System.Action onComplete = null);
    void PlayDaggerParry();
    void StopDaggerParry();

    void PlayHealStart();
    void PlayHealEnd(bool interrupted, System.Action onComplete = null);
    void PlayHurt();
    void PlayDeath();
}
