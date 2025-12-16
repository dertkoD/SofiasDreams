public interface IJumpAttack
{
    bool IsAttacking { get; }
    float CurrentDamage { get; }
    bool Request(AttackMode mode);
    void Interrupt();
}
