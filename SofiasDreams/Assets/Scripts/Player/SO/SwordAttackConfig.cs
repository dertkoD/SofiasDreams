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
}
