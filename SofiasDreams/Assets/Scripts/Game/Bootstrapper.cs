using UnityEngine;
using Zenject;

public class Bootstrapper : MonoBehaviour
{
    [Inject] Spawner _spawner;

    public Vector3 startPos;

    [SerializeField] EnemySpawnPoint[] _enemySpawnPoints;

    void Start()
    {
        _spawner.SpawnPlayer(startPos);

        foreach (var sp in _enemySpawnPoints)
        {
            if (sp != null)
                _spawner.SpawnEnemy(sp);
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(startPos, new Vector3(1, 3, 1));
    }
}
