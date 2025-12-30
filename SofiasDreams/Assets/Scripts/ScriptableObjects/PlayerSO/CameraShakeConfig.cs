using UnityEngine;

[CreateAssetMenu(fileName = "CameraShakeConfig", menuName = "Configs/Camera Shake Config")]
public class CameraShakeConfig : ScriptableObject
{
    [Header("Common Attack (Small Shake)")]
    public float commonAttackForce = 0.5f;
    public bool attackWhitoutHit = true;

    [Header("Enemy Hit (Medium Shake)")]
    public float enemyHitForce = 1.0f;

    [Header("Damage Taken (Large Shake)")]
    public float damageTakenForce = 2.0f;
    
    [Header("Floor break")]
    public float floorBreakForce = 1.0f;

    [Header("Continuous Shakes")]
    public float healShakeForce = 0.1f;
    public float dashShakeForce = 0.1f;
    public float continuousShakeFrequency = 0.05f;

    [Header("Damage Vignette")]
    public float vignetteIntensity = 0.5f;
    public float vignetteDuration = 0.5f;
    public float vignetteSmoothness = 0.5f;
    public Color vignetteColor = Color.red;
}
