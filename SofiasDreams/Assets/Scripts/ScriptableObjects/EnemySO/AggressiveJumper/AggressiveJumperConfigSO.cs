using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "AggressiveJumperConfig",
    menuName = "Configs/Enemy/Aggressive Jumper")]
public class AggressiveJumperConfigSO : ScriptableObject
{
    [Header("Patrol")]
    public JumpProfile patrolJump = JumpProfile.Create(5f, 8f, 0.45f);
    [Min(0f)] public float patrolIdleBetweenJumps = 0.45f;

    [Header("Agro / Attack")]
    public JumpProfile attackJump = JumpProfile.Create(7.5f, 11f, 0.2f);
    [Min(0f)] public float attackCooldown = 1.35f;
    [Min(0f)] public float attackLeadDistance = 0.6f;

    [Header("Awareness")]
    [Min(0f)] public float forgetDelay = 2.5f;
    [Min(0f)] public float lostSightGrace = 0.35f;
    [Min(0.05f)] public float visionRescanInterval = 0.1f;

    [Header("Grounding")]
    public LayerMask groundMask = ~0;
    public Vector2 groundCheckOffset = new(0f, -0.45f);
    [Min(0.01f)] public float groundCheckRadius = 0.2f;

    [Header("Facing")]
    [Min(0.5f)] public float faceBlendSpeed = 14f;

    [Header("Debug")]
    public bool verboseLogs;

    [Serializable]
    public struct JumpProfile
    {
        [Min(0f)] public float horizontalVelocity;
        [Min(0f)] public float verticalVelocity;
        public Vector2 impulse;
        [Min(0f)] public float postJumpDelay;

        public static JumpProfile Create(float horizontal, float vertical, float postDelay = 0.3f)
        {
            return new JumpProfile
            {
                horizontalVelocity = horizontal,
                verticalVelocity = vertical,
                impulse = Vector2.zero,
                postJumpDelay = postDelay
            };
        }
    }
}
