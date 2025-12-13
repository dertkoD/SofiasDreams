using UnityEngine;

[CreateAssetMenu(fileName = "JumpingEnemyConfig", menuName = "Configs/Enemy/Jumping Enemy")]
public class JumpingEnemyConfigSO : ScriptableObject
{
    [Header("Ground check")]
    public LayerMask groundMask;
    [Min(0f)] public float groundCastDistance = 0.06f;
    [Range(0f, 1f)] public float minGroundNormalY = 0.7f;

    [Header("Patrol (jumping)")]
    public bool loopPath = true;
    [Min(0.01f)] public float waypointArriveDistance = 0.2f;
    [Min(0f)] public float patrolJumpCooldown = 0.15f;
    [Min(0f)] public float patrolJumpHeight = 1.5f;
    [Min(0f)] public float patrolJumpHorizontalSpeed = 3.5f;

    [Header("Aggro")]
    [Min(0f)] public float aggroForgetSeconds = 2.0f;
    [Min(0f)] public float aggroJumpCooldown = 0.05f;
    [Min(0f)] public float aggroJumpHeight = 2.2f;
    [Min(0f)] public float aggroJumpHorizontalSpeed = 5.0f;

    [Header("Return to patrol")]
    [Tooltip("If true - after forgetting player, returns to the nearest patrol waypoint. Otherwise returns to spawn position.")]
    public bool returnToNearestWaypoint = true;
    [Min(0.01f)] public float returnArriveDistance = 0.25f;
}

