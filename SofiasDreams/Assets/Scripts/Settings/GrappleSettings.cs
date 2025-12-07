using UnityEngine;

public struct GrappleSettings
{
    public float radius;
    public LayerMask grappleLayer;
    public LayerMask obstacleLayer;

    public float moveSpeed;
    public float stopDistance;
    public float arrivalClearance;
    public bool zeroGravityWhileGrappling;

    public float startupDelay;
    
    public float exitStrength;
    public float carryOverEntrySpeedFactor;

    public float maxExitSpeedX;
    public float maxExitSpeedY;

    public float exitBlendTime;
    public bool blendByVelocityLerp;

    public float hardLockDuration;
    public float softCarryMaxDuration;

    public float cooldown;
}
