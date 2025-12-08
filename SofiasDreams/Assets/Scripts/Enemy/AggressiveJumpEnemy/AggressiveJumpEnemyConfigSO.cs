using UnityEngine;

[CreateAssetMenu(
    fileName = "AggressiveJumpEnemyConfig",
    menuName = "Configs/Enemy/AggressiveJump")]
public class AggressiveJumpEnemyConfigSO : EnemyConfigSO
{
    [Header("Patrol jump")]
    [Min(0f)] public float patrolJumpHorizontalSpeed = 3.5f;
    [Min(0f)] public float patrolJumpUpVelocity = 7.5f;
    [Min(0f)] public float patrolJumpCooldown = 0.9f;

    [Header("Aggro jump")]
    [Min(0f)] public float aggroJumpHorizontalSpeed = 6.5f;
    [Min(0f)] public float aggroJumpUpVelocity = 10f;
    [Min(0f)] public float aggroJumpCooldown = 0.45f;
    [Min(0f)] public float attackLeadTime = 0.15f;

    [Header("Aggro logic")]
    [Min(0f)] public float aggroForgetDelay = 2.0f;
    [Min(0f)] public float hitAggroDuration = 3.0f;

    [Header("Ground probe")]
    [Min(0f)] public float groundProbeRadius = 0.12f;
    [Min(0f)] public float groundProbeDistance = 0.08f;
    public LayerMask groundMask = Physics2D.DefaultRaycastLayers;

    [Header("Patrol path")]
    public bool loopPatrol = true;
}
