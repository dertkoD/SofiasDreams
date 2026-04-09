using UnityEngine;

[CreateAssetMenu(menuName = "Configs/SwordAttackConfig")]
public class SwordAttackConfig : ScriptableObject
{
    [Header("Combo damages")]
    public float damage = 15f;
    public float[] damages = { 12f, 15f, 25f };

    [Header("Pogo (down-air bounce)")]
    [Tooltip("Upward impulse applied when down-air hits an enemy")]
    public float pogoForce = 12f;

    [Header("Combo timing")]
    [Tooltip("Extra time window (seconds) after 2nd hit animation event to still register the 3rd hit")]
    [Min(0)] public float comboFinisherBuffer = 0.15f;
    [Tooltip("Cooldown (seconds) after the 3rd combo hit before a new combo can start")]
    [Min(0)] public float comboCooldown = 0.5f;

    [Header("Charged attack")]
    [Tooltip("How long the attack button must be held to charge")]
    public float chargeTime = 0.6f;

    [Tooltip("Total hits dealt to an enemy on a charged attack hit (including the first)")]
    [Min(1)] public int chargedMaxHits = 3;

    [Tooltip("Delay (seconds) between each extra hit after the first")]
    [Min(0)] public float chargedHitInterval = 0.15f;
}
