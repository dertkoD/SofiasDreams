using UnityEngine;

[CreateAssetMenu(menuName = "VFX/Dissolve Settings", fileName = "DissolveVfxSettings")]
public class DissolveVfxSettingsSO : ScriptableObject
{
    [Header("Timing")]
    [Min(0.01f)] public float duration = 0.6f;
    public AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Dissolve")]
    [Range(0f, 1.1f)] public float startAmount = 0f;
    [Range(0f, 1.1f)] public float endAmount = 1.1f;

    [Range(0f, 200f)] public float dissolveScale = 30f;
    [Range(-2f, 2f)] public float verticalDisolve = 0.5f;
    [Range(0f, 50f)] public float spiralStrenght = 5f;

    [Header("Outline")]
    public bool animateOutlineThickness = true;
    [Min(0f)] public float outlineStartThickness = 0.0f;
    [Min(0f)] public float outlineEndThickness = 0.15f;

    public Color outlineColor = Color.magenta;

    [Tooltip("Множитель яркости цвета (HDR). Для Bloom обычно нужно 2..20+")]
    [Min(0f)] public float outlineIntensity = 8f;

    [Header("After")]
    public bool disableSpriteRendererOnFinish = true;
}
