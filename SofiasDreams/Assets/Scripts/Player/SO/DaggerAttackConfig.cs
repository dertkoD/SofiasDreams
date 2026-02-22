using UnityEngine;

[CreateAssetMenu(menuName = "Configs/Player/DaggerAttack", fileName = "DaggerAttackConfig")]
public class DaggerAttackConfig : ScriptableObject
{
    [Header("Combo damage")]
    [Min(1)] public float damage1 = 8f;
    [Min(1)] public float damage2 = 8f;
    [Min(1)] public float superDamage = 25f;

    [Header("Combo timing (длительность анимации каждого удара)")]
    [Min(0)] public float attack1Duration = 0.35f;
    [Min(0)] public float attack2Duration = 0.35f;

    [Header("Super attack — float effect")]
    [Min(0)] public float superLaunchForce = 12f;
    [Min(0)] public float floatDuration = 0.6f;
    [Range(0f, 1f)] public float floatGravityScale = 0.1f;
}
