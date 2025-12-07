public readonly struct EnemyDiedSignal
{
    public readonly EnemyFacade Enemy;

    public EnemyDiedSignal(EnemyFacade enemy)
    {
        Enemy = enemy;
    }
}
