using UnityEngine;

[CreateAssetMenu(menuName = "Configs/PlayerGrapple")]
public class PlayerGrappleConfig : ScriptableObject
{
    [Header("Targeting")]
    public float radius = 8f;
    public LayerMask grappleLayer;
    public LayerMask obstacleLayer;

    [Header("Zip (straight-line)")]
    [Tooltip("Constant travel speed toward the hook.")]
    public float moveSpeed = 27f;
    [Tooltip("Stop when within this distance of the target to avoid contact jitter.")]
    public float stopDistance = 0f;
    [Tooltip("Stand off from the point by this much to avoid overlap on arrival.")]
    public float arrivalClearance = 0f;
    [Tooltip("Set true to null gravity during zip for a perfect straight line.")]
    public bool zeroGravityWhileGrappling = true;
    
    [Header("Timings")]
    [Tooltip("Pause before starting the zip (for frames 1–2).")]
    public float startupDelay = 0.08f;

    [Header("Exit Jump (fixed strength, direction = travel vector)")]
    [Tooltip("Total exit speed magnitude. Direction = saved travel dir (start->target).")]
    public float exitStrength = 16f;
    [Tooltip("Optionally add some of the entry speed to the exit (0 = none).")]
    [Range(0f, 1f)] public float carryOverEntrySpeedFactor = 0f;

    [Header("Exit Limits (per-axis)")]
    [Tooltip("Max horizontal exit speed. 0 or negative = no horizontal clamp.")]
    public float maxExitSpeedX = 11f;
    [Tooltip("Max vertical exit speed. 0 or negative = no vertical clamp.")]
    public float maxExitSpeedY = 16f;

    [Header("Exit Smoothing")]
    [Tooltip("Blend time from zip velocity into exit velocity (seconds). 0 = instant.")]
    public float exitBlendTime = 0.05f;
    [Tooltip("If true, lerp velocity over exitBlendTime; if false, ramp by impulses.")]
    public bool blendByVelocityLerp = true;

    [Header("Momentum Windows")]
    [Tooltip("Hard lock: duration where exit velocity is enforced every frame.")]
    public float hardLockDuration = 0.12f;
    [Tooltip("Soft carry: max time momentum is preserved until steer/ground/timeout.")]
    public float softCarryMaxDuration = 0.50f;

    [Header("Timing / Control")]
    public float cooldown = 0.1f;
}
