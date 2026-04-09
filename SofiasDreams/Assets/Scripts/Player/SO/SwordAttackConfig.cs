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

    [Tooltip("Max hits a single enemy can take during charged attack")]
    [Min(1)] public int chargedMaxHits = 3;

    [Tooltip("Total active hitbox duration of the charged attack (seconds)")]
    public float chargedHitDuration = 4f;
}
