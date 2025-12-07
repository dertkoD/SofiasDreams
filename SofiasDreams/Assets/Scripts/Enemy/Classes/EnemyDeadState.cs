public class EnemyDeadState : IEnemyState
{
    readonly EnemyMovement _movement;

    public EnemyDeadState(EnemyMovement movement)
    {
        _movement = movement;
    }

    public void Enter()
    {
        _movement.Stop();
    }

    public void Tick() { }

    public void Exit() { }
}
