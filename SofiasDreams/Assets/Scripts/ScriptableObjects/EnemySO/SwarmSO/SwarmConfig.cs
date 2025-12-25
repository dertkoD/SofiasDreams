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
    public float visionRadius = 10f;       
    public float fleeDistance = 3.0f;     // DEPRECATED logic-wise, replaced by maintainDistance
    public float maintainDistance = 8.0f; // Дистанция, которую пытаемся держать (убегаем, если ближе)
    public float aggroForgetSeconds = 3.0f;
    public float patrolPathSearchRadius = 50f;
    public float waypointArriveDistance = 1.0f;
    
    // Moved from MinionConfig
    public float minionOrbitRadius = 9.0f;

    [Header("Spawning (minions)")]
    public int maxMinions = 3;
    public float spawnInterval = 1.5f;    
    public int poolInitialSize = 5;

    [Header("Damage")]
    public int contactDamage = 1;

    [Header("Refs")]
    public MinionConfig minionConfig;
    public GameObject minionPrefab; 
}
