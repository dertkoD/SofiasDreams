using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class Bootstrapper : MonoBehaviour
{
    [Inject] Spawner _spawner;
    [Inject] SignalBus _bus;
    [Inject] IBonfireService _bonfire;

    public Vector3 startPos;

    [SerializeField] EnemySpawnPoint[] _enemySpawnPoints;

    readonly List<EnemyFacade> _spawnedEnemies = new();

    void OnEnable()
    {
        _bus.Subscribe<BonfireEnemiesRespawnRequested>(OnRespawnEnemies);
        _bus.Subscribe<BonfireRespawnRequested>(OnRespawnPlayer);
    }

    void OnDisable()
    {
        _bus.TryUnsubscribe<BonfireEnemiesRespawnRequested>(OnRespawnEnemies);
        _bus.TryUnsubscribe<BonfireRespawnRequested>(OnRespawnPlayer);
    }

    void Start()
    {
#if UNITY_EDITOR
        PlayerPrefs.DeleteKey("checkpoint.bonfireId");
        PlayerPrefs.DeleteKey("checkpoint.x");
        PlayerPrefs.DeleteKey("checkpoint.y");
        PlayerPrefs.DeleteKey("checkpoint.z");
#endif
        // Spawn player at saved checkpoint if available
        var p = (_bonfire != null && _bonfire.HasCheckpoint) ? startPos /* will be replaced by respawn signal later */ : startPos;
        // We don't have direct access to checkpoint position here (kept inside BonfireService),
        // so we rely on BonfireService calling BonfireRespawnRequested on death.
        // Initial spawn uses startPos.
        _spawner.SpawnPlayer(startPos);

        SpawnEnemies();
    }

    void SpawnEnemies()
    {
        foreach (var sp in _enemySpawnPoints)
        {
            if (sp == null) continue;
            var e = _spawner.SpawnEnemy(sp);
            if (e != null) _spawnedEnemies.Add(e);
        }
    }

    void ClearEnemies()
    {
        for (int i = 0; i < _spawnedEnemies.Count; i++)
        {
            if (_spawnedEnemies[i] != null)
                Destroy(_spawnedEnemies[i].gameObject);
        }
        _spawnedEnemies.Clear();
    }

    void OnRespawnEnemies(BonfireEnemiesRespawnRequested _)
    {
        ClearEnemies();
        SpawnEnemies();
    }

    // This spawns a NEW player on respawn (simple + avoids needing a "revive" API)
    void OnRespawnPlayer(BonfireRespawnRequested s)
    {
        // You might end up with an old dead player still in scene.
        // If that happens, tell me and I’ll add a clean "current player tracking + destroy old" step.
        _spawner.SpawnPlayer(s.Position);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(startPos, new Vector3(1, 3, 1));
    }
}