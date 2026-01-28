using UnityEngine;

[CreateAssetMenu(fileName = "BlinkingSettings", menuName = "SO/BlinkingSettings")]
public class BlinkingSettingsSO : ScriptableObject
{
    [Header("Blink Settings")]
    public float blinkDuration = 0.1f;
    public float outlineThickness = 1f;
    public float dissolveAmount = 1.1f;
    public float outlineIntensity = 1f;
    public float verticalDissolve = 1.1f;
    public Color outlineColor = Color.white;
}
