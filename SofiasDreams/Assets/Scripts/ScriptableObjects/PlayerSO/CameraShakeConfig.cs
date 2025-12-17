using UnityEngine;

[CreateAssetMenu(fileName = "CameraShakeConfig", menuName = "Configs/Camera Shake Config")]
public class CameraShakeConfig : ScriptableObject
{
    [Header("Air Attack (Small Shake)")]
    public float airAttackForce = 0.5f;

    [Header("Enemy Hit (Medium Shake)")]
    public float enemyHitForce = 1.0f;

    [Header("Damage Taken (Large Shake)")]
    public float damageTakenForce = 2.0f;
}
