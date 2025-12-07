public interface IHealth
{
    bool IsAlive { get; }
    bool IsInvincible { get; }

    void ApplyDamage(DamageInfo info);
    void TakeDamage(int amount);
    bool CanHeal();
    void Heal(int amount);

    int CurrentHP { get; }
    int MaxHP { get; }
}
