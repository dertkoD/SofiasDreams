using UnityEngine;

[CreateAssetMenu(fileName = "SwarmConfig", menuName = "Configs/Swarm")]
public class SwarmConfig : ScriptableObject
{
    [Header("Movement (NavMesh)")]
    public float patrolSpeed = 2.0f;
    public float aggroSpeed = 2.0f; // Speed when keeping distance/following
    public float fleeSpeed = 4.0f;
    public float acceleration = 8.0f;
    public float angularSpeed = 120f;
    public float stoppingDistance = 0.5f;

    [Header("Behavior")]
    public float visionRadius = 8f;       // «вижу игрока»
    public float fleeDistance = 4.0f;     // Если игрок ближе, чем это, то убегать
    public float maintainDistance = 6.0f; // Предпочтительная дистанция
    public float aggroForgetSeconds = 3.0f;
    public float patrolPathSearchRadius = 50f;
    public float waypointArriveDistance = 1.0f;

    [Header("Spawning (minions)")]
    public int maxMinions = 3;
    public float spawnInterval = 1.5f;    // Интервал между спавном
    public int poolInitialSize = 5;

    [Header("Damage")]
    public int contactDamage = 1;

    [Header("Refs")]
    public MinionConfig minionConfig;
    public GameObject minionPrefab; // квадрат с MinionController
}
