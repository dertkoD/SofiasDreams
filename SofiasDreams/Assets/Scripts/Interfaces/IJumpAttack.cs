public interface IJumpAttack
{
    bool IsAttacking { get; }
    float CurrentDamage { get; }
    void Request(AttackMode mode);
    void Interrupt();
}
