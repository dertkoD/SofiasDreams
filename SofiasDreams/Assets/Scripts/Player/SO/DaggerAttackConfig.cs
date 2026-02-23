using UnityEngine;

[CreateAssetMenu(menuName = "Configs/Player/DaggerAttack", fileName = "DaggerAttackConfig")]
public class DaggerAttackConfig : ScriptableObject
{
    [Header("Combo damage")]
    [Min(1)] public float damage1 = 8f;
    [Min(1)] public float damage2 = 8f;
    [Min(1)] public float superDamage = 25f;

    [Header("Charged attack")]
    [Tooltip("How long the attack button must be held to charge")]
    [Min(0.1f)] public float chargeTime = 0.5f;

    [Header("Charged attack — launch")]
    [Tooltip("Upward velocity applied to the player")]
    [Min(0)] public float playerLaunchForce = 12f;
    [Tooltip("Upward impulse applied to the enemy on hit")]
    [Min(0)] public float enemyLaunchForce = 14f;

    [Header("Charged attack — float gravity")]
    [Tooltip("Gravity scale during float (lower = floatier)")]
    [Range(0f, 1f)] public float floatGravityScale = 0.1f;
    [Tooltip("How long the reduced gravity lasts (seconds)")]
    [Min(0)] public float floatGravityDuration = 0.3f;
}
