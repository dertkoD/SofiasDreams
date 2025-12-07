using UnityEngine;
using Zenject;

public class Spawner
{
    readonly PlayerFactory _playerFactory;
    readonly EnemyFactory _enemyFactory;
    readonly SignalBus _bus;

    public Spawner(PlayerFactory playerFactory, EnemyFactory enemyFactory, SignalBus bus)
    {
        _playerFactory = playerFactory;
        _enemyFactory  = enemyFactory;
        _bus           = bus;
    }

    public PlayerFacade SpawnPlayer(Vector3 pos)
    {
        var player = _playerFactory.Create();
        player.transform.position = pos;
        _bus.Fire(new PlayerSpawned { facade = player });
        return player;
    }

    public EnemyFacade SpawnEnemy(EnemySpawnPoint spawnPoint)
    {
        var enemy = _enemyFactory.Create();
        enemy.transform.position = spawnPoint.Position;

        var path = spawnPoint.PatrolPath;
        if (path != null)
            enemy.SetPatrolPath(path);
        
        return enemy;
    }
}
