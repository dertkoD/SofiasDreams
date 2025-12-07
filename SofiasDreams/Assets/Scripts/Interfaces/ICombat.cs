public interface ICombat
{
    bool IsAttacking { get; }
    void RequestAttack();
    void Interrupt();
}
