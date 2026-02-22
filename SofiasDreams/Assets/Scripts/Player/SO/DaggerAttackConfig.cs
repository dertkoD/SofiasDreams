using UnityEngine;

[CreateAssetMenu(menuName = "Configs/Player/DaggerAttack", fileName = "DaggerAttackConfig")]
public class DaggerAttackConfig : ScriptableObject
{
    [Header("Combo damage")]
    [Min(1)] public float damage1 = 8f;
    [Min(1)] public float damage2 = 8f;
    [Min(1)] public float superDamage = 25f;

    [Header("Combo timing")]
    [Tooltip("Window after a hit finishes during which the next combo input is accepted")]
    [Min(0)] public float comboWindowTime = 0.6f;

    [Header("Super attack — float effect")]
    [Min(0)] public float superLaunchForce = 12f;
    [Min(0)] public float floatDuration = 0.6f;
    [Range(0f, 1f)] public float floatGravityScale = 0.1f;
}
