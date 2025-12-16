public readonly struct EnemyDiedSignal
{
    public readonly EnemyFacade Enemy;
    public readonly bool KilledByPlayer;

    public EnemyDiedSignal(EnemyFacade enemy, bool killedByPlayer = false)
    {
        Enemy = enemy;
        KilledByPlayer = killedByPlayer;
    }
}
