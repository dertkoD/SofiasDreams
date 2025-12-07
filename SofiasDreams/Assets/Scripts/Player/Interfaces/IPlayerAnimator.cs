public interface IPlayerAnimator
{
    void SetMoveSpeed(float speed01);
    void SetGrounded(bool grounded);
    
    void PlayAttack(int index);
    void PlayUpAttack();
    void PlayAirForwardAttack();   
    void PlayAirDownAttack();         
    void PlayAirUpAttack(); 
    
    void PlayHealStart();
    void PlayHealEnd(bool interrupted);
    void PlayHurt();
    void PlayDeath();
}
