public interface IUpAttack
{
    bool IsAttacking { get; }
    float CurrentDamage { get; }
    void Request();
    void Interrupt();
}
