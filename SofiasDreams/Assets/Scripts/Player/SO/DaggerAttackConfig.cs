using UnityEngine;

[CreateAssetMenu(menuName = "Configs/Player/DaggerAttack", fileName = "DaggerAttackConfig")]
public class DaggerAttackConfig : ScriptableObject
{
    [Header("Combo damage")]
    [Min(1)] public float damage1 = 8f;
    [Min(1)] public float damage2 = 8f;
    [Min(1)] public float superDamage = 25f;

    [Header("Charged attack")]
    [Tooltip("Cooldown between charged attacks (seconds)")]
    [Min(0)] public float chargedCooldown = 1.5f;

    [Header("Charged attack — launch")]
    [Tooltip("Upward velocity applied to the player")]
    [Min(0)] public float playerLaunchForce = 12f;

    [Header("Charged attack — float gravity")]
    [Tooltip("Gravity scale during float (lower = floatier)")]
    [Range(0f, 1f)] public float floatGravityScale = 0.1f;
    [Tooltip("How long the reduced gravity lasts (seconds)")]
    [Min(0)] public float floatGravityDuration = 0.3f;

    [Header("Air attack hover")]
    [Tooltip("Gravity scale while performing dagger air attacks (0 = full freeze)")]
    [Range(0f, 1f)] public float airHoverGravityScale = 0f;

    [Header("Momentum")]
    public int segmentsPerLevel = 5;
    public int maxLevels = 3;
    [Tooltip("Attack speed bonus per level (0.2 = +20%)")]
    public float speedBonusPerLevel = 0.2f;
    [Tooltip("Segments added by a normal hit")]
    public int normalHitSegments = 1;
    [Tooltip("Segments added by a backstab hit")]
    public int backstabSegments = 1;
    [Tooltip("Segments added by a successful parry (= 1 full level)")]
    public int parrySegments = 5;
    [Tooltip("Seconds before segments start decaying")]
    public float decayDelay = 5f;
    [Tooltip("Segments lost per second during decay")]
    public float decayRate = 1f;

    [Header("Parry")]
    [Tooltip("Duration of the parry window after pressing RMB")]
    [Min(0.01f)] public float parryWindow = 0.25f;
    [Tooltip("Stun duration applied to the enemy on successful parry")]
    [Min(0)] public float parryStunDuration = 1f;
    [Tooltip("How far behind the enemy the player teleports")]
    [Min(0)] public float parryTeleportOffset = 1.5f;
}
