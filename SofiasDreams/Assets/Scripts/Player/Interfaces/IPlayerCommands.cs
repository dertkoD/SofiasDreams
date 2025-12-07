public interface  IPlayerCommands
{
    void Move(float x);
    void Stop();
    void Jump();
    void JumpRelease();
    void Attack();      
    void UpAttack();    

    void ForwardJumpAttack(); 
    void UpJumpAttack();      
    void DownJumpAttack();    

    void HealBegin();
    void HealCancel();
    
    void Dash();

    void Grapple();

    void DropPlatform();

    void ApplyDamage(DamageInfo info);
}
