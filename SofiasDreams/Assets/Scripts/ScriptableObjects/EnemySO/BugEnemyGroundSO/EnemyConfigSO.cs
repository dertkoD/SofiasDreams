using UnityEngine;

[CreateAssetMenu(fileName = "EnemyConfig", menuName = "Configs/Enemy/PatrolAreaEnemy")]
public class EnemyConfigSO : ScriptableObject
{
    [Header("Movement")]
    [Min(0f)] public float moveSpeed = 3f;
    [Min(0.01f)] public float destinationTolerance = 0.1f;

    [Header("Patrol")]
    [Min(0f)] public float minPauseAtPoint = 0.5f;
    [Min(0f)] public float maxPauseAtPoint = 2f;
    [Min(0.1f)] public float newPointSearchRadius = 2f;
}