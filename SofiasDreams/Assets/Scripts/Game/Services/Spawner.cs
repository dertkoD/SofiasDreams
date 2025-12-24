using UnityEngine;
using Zenject;

public class Spawner
{
    readonly PlayerFactory _playerFactory;
    readonly GroundEnemyFactory _groundEnemyFactory;
    readonly FlyingEnemyFactory _flyingEnemyFactory;
    readonly JumpingEnemyFactory _jumpingEnemyFactory;
    readonly WormEnemyFactory _wormEnemyFactory;
    readonly SignalBus _bus;
    PlayerFacade _currentPlayer;

    
    public Spawner(
        PlayerFactory playerFactory,
        GroundEnemyFactory groundEnemyFactory,
        FlyingEnemyFactory flyingEnemyFactory, 
        JumpingEnemyFactory jumpingEnemyFactory,
        WormEnemyFactory wormEnemyFactory,
        SignalBus bus)
    {
        _playerFactory       = playerFactory;
        _groundEnemyFactory  = groundEnemyFactory;
        _flyingEnemyFactory  = flyingEnemyFactory;
        _jumpingEnemyFactory = jumpingEnemyFactory;
        _wormEnemyFactory    = wormEnemyFactory;
        _bus           = bus;
    }

    public PlayerFacade SpawnPlayer(Vector3 pos)
    {
        if (_currentPlayer != null)
        {
            GameObject.Destroy(_currentPlayer.gameObject);
            _currentPlayer = null;
        }

        var player = _playerFactory.Create();
        player.transform.position = pos;

        // ---- Zero physics state on spawn ----
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = player.GetComponentInChildren<Rigidbody2D>();

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;

            // Optional: also clear residual forces/jitter
            rb.Sleep();
            rb.WakeUp();
        }
        // ------------------------------------

        _currentPlayer = player;

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
            case EnemyMovementMode.Jumping:
                enemy = _jumpingEnemyFactory.Create();
                break;
            case EnemyMovementMode.Worm:
                enemy = _wormEnemyFactory.Create();
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
