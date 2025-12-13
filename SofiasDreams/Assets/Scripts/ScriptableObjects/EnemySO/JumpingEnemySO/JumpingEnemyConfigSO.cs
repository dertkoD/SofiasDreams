using UnityEngine;

[CreateAssetMenu(fileName = "JumpingEnemyConfig", menuName = "Configs/Enemy/Jumping Enemy")]
public class JumpingEnemyConfigSO : ScriptableObject
{
    [Header("Ground check")]
    public LayerMask groundMask;
    [Header("Ground check (like player Jumper2D)")]
    [Tooltip("Upward velocity that immediately marks jump as airborne before contacts separate.")]
    [Min(0f)] public float leaveGroundVelocity = 0.05f;
    [Tooltip("Filter by surface normal angle (degrees). 90 is 'up'.")]
    [Range(0f, 180f)] public float minGroundNormalAngle = 80f;
    [Range(0f, 180f)] public float maxGroundNormalAngle = 100f;

    [Header("Grounded stability")]
    [Min(0f)] public float groundedVelocityEpsilon = 0.05f;

    [Header("Step assist (stairs / small ledges)")]
    public bool stepAssistEnabled = true;
    [Tooltip("Layers considered as obstacles for step detection (usually same as ground).")]
    public LayerMask obstacleMask;
    [Min(0f)] public float obstacleCheckDistance = 0.35f;
    [Tooltip("Ray origin offset from enemy pivot for obstacle check.")]
    public Vector2 obstacleRayOffset = new Vector2(0f, 0.25f);
    [Min(0f)] public float targetUpThreshold = 0.15f;
    [Min(0f)] public float targetUpMargin = 0.25f;
    [Min(0f)] public float extraJumpHeightOnObstacle = 0.6f;
    [Min(0f)] public float extraHorizontalSpeedOnObstacle = 0.0f;
    [Min(0f)] public float maxAssistedJumpHeight = 0f; // 0 => no clamp

    [Header("Patrol (jumping)")]
    public bool loopPath = true;
    [Min(0.01f)] public float waypointArriveDistance = 0.2f;
    [Min(0f)] public float patrolJumpCooldown = 0.15f;
    [Min(0f)] public float patrolJumpHeight = 1.5f;
    [Min(0f)] public float patrolJumpHorizontalSpeed = 3.5f;
    [Min(0f)] public float landingStunSeconds = 0.10f;

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

