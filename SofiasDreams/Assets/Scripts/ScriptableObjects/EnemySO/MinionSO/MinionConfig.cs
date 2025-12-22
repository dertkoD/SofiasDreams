using UnityEngine;

[CreateAssetMenu(fileName = "MinionConfig", menuName = "Configs/Minion")]
public class MinionConfig : ScriptableObject
{
    [Header("Movement (NavMesh)")]
    public float patrolSpeed = 3.5f;
    public float aggroSpeed = 5.0f;
    public float acceleration = 12.0f;
    public float angularSpeed = 360f;
    public float stoppingDistance = 0.5f;

    [Header("Patrol (Orbit)")]
    public float orbitRadius = 3.0f;
    public float orbitSpeed = 2.0f; // radians/sec

    [Header("Roles - Aggressor")]
    public float attackDistance = 3.0f; // Try to get this close
    public float attackBackoffDistance = 2.0f; // Back off if too close

    [Header("Roles - Support")]
    public float supportDistance = 6.0f;
    public float supportLateralSpread = 2.5f; // Spread out to sides
    public float supportFireIntervalMin = 1.5f;
    public float supportFireIntervalMax = 2.5f;

    [Header("Shooting")]
    public float fireCooldown = 0.8f;
    public float initialFireDelay = 0.5f;

    [Header("Behavior")]
    public float forgetTime = 3.0f; // Time to return to patrol after losing player
    public float visionRadius = 8.0f;

    [Header("Refs")]
    public GameObject bulletPrefab;
}
