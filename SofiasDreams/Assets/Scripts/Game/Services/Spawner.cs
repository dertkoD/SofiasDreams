using UnityEngine;
using Zenject;

public class Spawner
{
    readonly PlayerFactory _playerFactory;
    readonly GroundEnemyFactory _groundEnemyFactory;
    readonly FlyingEnemyFactory _flyingEnemyFactory;
    readonly SignalBus _bus;
    
    public Spawner(
        PlayerFactory playerFactory,
        GroundEnemyFactory groundEnemyFactory,
        FlyingEnemyFactory flyingEnemyFactory, 
        SignalBus bus)
    {
        _playerFactory       = playerFactory;
        _groundEnemyFactory  = groundEnemyFactory;
        _flyingEnemyFactory  = flyingEnemyFactory;
        _bus           = bus;
    }

    public PlayerFacade SpawnPlayer(Vector3 pos)
    {
        var player = _playerFactory.Create();
        player.transform.position = pos;
        _bus.Fire(new PlayerSpawned { facade = player });
        return player;
    }

    public EnemyFacade SpawnEnemy(EnemySpawnPoint sp)
    {
        if (sp == null)
            return null;

        EnemyFacade enemy;

        switch (sp.Kind)
        {
            case EnemyMovementMode.Planar2D:
                enemy = _flyingEnemyFactory.Create();
                break;
            case EnemyMovementMode.GroundOnly:
            default:
                enemy = _groundEnemyFactory.Create();
                break;
        }

        var tr = enemy.transform;
        tr.position = sp.transform.position;

        var path = sp._patrolPath;
        if (path != null)
            enemy.SetPatrolPath(path);   

        return enemy;
    }
}
