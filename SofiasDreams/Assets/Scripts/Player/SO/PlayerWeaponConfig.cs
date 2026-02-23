using UnityEngine;

[CreateAssetMenu(menuName = "Configs/Player/Weapon", fileName = "PlayerWeaponConfig")]
public class PlayerWeaponConfig : ScriptableObject
{
    [Min(1)] public int baseDamage = 10;
    public LayerMask targetLayers;

    [Tooltip("Knockback force applied to targets. -1 = use target's default")]
    public float knockbackForce = -1f;
}
