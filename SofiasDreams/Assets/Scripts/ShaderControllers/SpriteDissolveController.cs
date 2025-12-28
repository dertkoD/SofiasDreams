using System;
using System.Collections;
using UnityEngine;

public class SpriteDissolveController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    private MaterialPropertyBlock mpb;
    private Coroutine routine;

    private static readonly int DisolveAmountId     = Shader.PropertyToID("_DisolveAmount");
    private static readonly int OutlineThicknessId  = Shader.PropertyToID("_OutlineThickness");
    private static readonly int OutlineColorId      = Shader.PropertyToID("_OutlineColor");
    private static readonly int DisolveScaleId      = Shader.PropertyToID("_DisolveScale");
    private static readonly int VerticalDisolveId   = Shader.PropertyToID("_VerticalDisolve");
    private static readonly int SpiralStrenghtId    = Shader.PropertyToID("_SpiralStrenght");

    private void Awake()
    {
        if (!spriteRenderer) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        mpb ??= new MaterialPropertyBlock();
    }

    public void Play(DissolveVfxSettingsSO s, Action onFinished = null)
    {
        if (!spriteRenderer || !s) return;

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Routine(s, onFinished));
    }

    public void ApplyInstant(DissolveVfxSettingsSO s, float amount, float thickness)
    {
        if (!spriteRenderer || !s) return;

        spriteRenderer.enabled = true;

        spriteRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat(DisolveScaleId, s.dissolveScale);
        mpb.SetFloat(VerticalDisolveId, s.verticalDisolve);
        mpb.SetFloat(SpiralStrenghtId, s.spiralStrenght);
        mpb.SetColor(OutlineColorId, s.outlineColor * s.outlineIntensity);
        mpb.SetFloat(DisolveAmountId, amount);
        mpb.SetFloat(OutlineThicknessId, thickness);
        spriteRenderer.SetPropertyBlock(mpb);
    }

    private IEnumerator Routine(DissolveVfxSettingsSO s, Action onFinished)
    {
        spriteRenderer.enabled = true;

        // constants
        spriteRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat(DisolveScaleId, s.dissolveScale);
        mpb.SetFloat(VerticalDisolveId, s.verticalDisolve);
        mpb.SetFloat(SpiralStrenghtId, s.spiralStrenght);
        mpb.SetColor(OutlineColorId, s.outlineColor * s.outlineIntensity);
        spriteRenderer.SetPropertyBlock(mpb);

        float dur = Mathf.Max(0.01f, s.duration);
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            float k = s.curve.Evaluate(Mathf.Clamp01(t));

            float amount = Mathf.Lerp(s.startAmount, s.endAmount, k);
            float thickness = s.animateOutlineThickness
                ? Mathf.Lerp(s.outlineStartThickness, s.outlineEndThickness, k)
                : s.outlineEndThickness;

            spriteRenderer.GetPropertyBlock(mpb);
            mpb.SetFloat(DisolveAmountId, amount);
            mpb.SetFloat(OutlineThicknessId, thickness);
            spriteRenderer.SetPropertyBlock(mpb);

            yield return null;
        }

        routine = null;

        if (s.disableSpriteRendererOnFinish)
            spriteRenderer.enabled = false;

        onFinished?.Invoke();
    }
}
