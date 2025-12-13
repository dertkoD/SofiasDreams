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

    [Header("Air control (horizontal while in air)")]
    [Min(0f)] public float airControlAcceleration = 60f;
    [Tooltip("If > 0, limits how fast X can change per fixed tick. 0 => use acceleration only.")]
    [Min(0f)] public float airControlMaxDeltaVX = 0f;

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

