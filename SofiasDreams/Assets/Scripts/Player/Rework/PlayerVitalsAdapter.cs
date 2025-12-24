public class PlayerVitalsAdapter : IPlayerVitals
{
    readonly Health _health;
    readonly Healer _healer;

    public PlayerVitalsAdapter(Health health, Healer healer)
    {
        _health = health;
        _healer = healer;
    }

    public void RestoreAtBonfire()
    {
        _health.Heal(20);
        _healer.RestoreChargesToMax();
    }
}