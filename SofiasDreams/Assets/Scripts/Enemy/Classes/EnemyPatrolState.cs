public sealed class EnemyPatrolState : IEnemyState
{
    readonly EnemyPatrolController _patrol;

    public EnemyPatrolState(EnemyPatrolController patrol)
    {
        _patrol = patrol;
    }

    public void Enter()
    {
        _patrol.BeginPatrol();
    }

    public void Tick()
    {
        _patrol.Tick();
    }

    public void Exit()
    {
        _patrol.StopPatrol();
    }
}