using UnityEngine;

[CreateAssetMenu(menuName = "Configs/Combat/Hit Reaction", fileName = "HitReactionConfig")]
public class HitReactionConfig : ScriptableObject
{
    [Header("Stun / Knockback")]
    public float hitStun = 0.15f;        
    public float knockbackForce = 5f;      

    [Header("Damage feedback physics")]
    public float dragDuringStun = 20f;
    public float knockbackX = 4.5f;
    public float knockbackY = 2.0f;
    public bool zeroHorizontalBeforeKnockback = true;

    [Header("Flash")]
    public float flashInterval = 0.05f;
    public Color flashColor = Color.white;
    public bool useToggleBlinkIfWhite = true;
}
