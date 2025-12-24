public class EnemyCombatGate : IEnemyCombatGate
{
    public static bool IsBonfireSafe { get; private set; }
    public void SetBonfireSafe(bool isSafe) => IsBonfireSafe = isSafe;
}