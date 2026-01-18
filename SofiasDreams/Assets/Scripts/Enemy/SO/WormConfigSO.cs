using UnityEngine;

[CreateAssetMenu(fileName = "WormConfig", menuName = "Configs/Enemy/Worm")]
public class WormConfigSO : ScriptableObject
{
    [Header("Patrol")]
    [Min(0f)] public float patrolSpeed = 2f;
    [Min(0f)] public float patrolAcceleration = 20f;
    [Min(0f)] public float patrolDeceleration = 25f;
    [Min(0f)] public float patrolTurnWaitTime = 0.5f; 

    [Header("Aggro / Windup")]
    [Min(0f)] public float windupTime = 0.12f;
    [Min(0f)] public float aggroForgetSeconds = 2.0f;

    [Header("Spin / Charge")]
    [Min(0f)] public float chargeSpeed = 10f;
    [Min(0f)] public float chargeAcceleration = 100f;
    [Min(0f)] public float spinMinDuration = 0.10f;

    [Header("Bounce (Hit Wall/Player)")]
    [Min(0f)] public float bounceArcDistance = 1.5f;
    [Min(0f)] public float bounceArcHeight = 0.8f;

    [Header("Stun")]
    [Min(0f)] public float stunDuration = 0.6f;
    [Min(0f)] public float stunDrag = 20f;

    [Header("Sensors & Physics")]
    public LayerMask solidLayers;
    public LayerMask playerLayer;
    
    [Header("Ground Check")]
    public LayerMask groundMask;
    [Range(0f, 180f)] public float minGroundNormalAngle = 80f;
    [Range(0f, 180f)] public float maxGroundNormalAngle = 100f;
    [Min(0f)] public float leaveGroundVelocity = 0.1f;

    [Header("Jump Over Reaction")]
    [Min(0f)] public float jumpOverRayHeight = 5f;
    [Min(1f)] public float jumpOverSpeedMultiplier = 2f;
}
