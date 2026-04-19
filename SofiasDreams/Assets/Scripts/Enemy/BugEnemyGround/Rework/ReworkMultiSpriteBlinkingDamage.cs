using System.Collections;
using UnityEngine;

/// <summary>
/// Blink variant for the ReworkBugEnemyGround.
/// Listens to the single enemy <see cref="Health"/> and plays the blink effect
/// on every assigned SpriteRenderer (head + body) in sync whenever damage is taken.
/// Does not replace <see cref="BlinkingDamage"/> — used alongside split-sprite enemies.
/// </summary>
public class ReworkMultiSpriteBlinkingDamage : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("All sprite renderers that should blink together (head + body).")]
    [SerializeField] private SpriteRenderer[] spriteRenderers;
    [SerializeField] private BlinkingSettingsSO settings;

    [Header("Health source")]
    [Tooltip("If empty, will search the GameObject and its parents for a Health component.")]
    [SerializeField] private Health health;

    private MaterialPropertyBlock _mpb;
    private Coroutine _blinkRoutine;
    private int _lastHealth;

    private static readonly int OutlineThicknessId  = Shader.PropertyToID("_OutlineThickness");
    private static readonly int OutlineColorId      = Shader.PropertyToID("_OutlineColor");
    private static readonly int DisolveAmountId     = Shader.PropertyToID("_DisolveAmount");
    private static readonly int VerticalDisolveId   = Shader.PropertyToID("_VerticalDisolve");

    private void Awake()
    {
        _mpb = new MaterialPropertyBlock();

        if (spriteRenderers == null || spriteRenderers.Length == 0)
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);

        if (!health)
        {
            health = GetComponent<Health>();
            if (!health) health = GetComponentInParent<Health>();
        }
    }

    private void Start()
    {
        if (health)
        {
            _lastHealth = health.CurrentHP;
            health.OnHealthChanged += OnHealthChanged;
        }
    }

    private void OnDestroy()
    {
        if (health)
            health.OnHealthChanged -= OnHealthChanged;
    }

    private void OnHealthChanged()
    {
        if (!health) return;

        if (health.CurrentHP < _lastHealth)
            Blink();

        _lastHealth = health.CurrentHP;
    }

    public void Blink()
    {
        if (!settings || spriteRenderers == null || spriteRenderers.Length == 0)
        {
            Debug.LogWarning("[ReworkMultiSpriteBlinkingDamage] Missing settings or sprite renderers!");
            return;
        }

        if (health != null && !health.IsAlive) return;

        if (_blinkRoutine != null) StopCoroutine(_blinkRoutine);
        _blinkRoutine = StartCoroutine(BlinkRoutine());
    }

    private IEnumerator BlinkRoutine()
    {
        ApplyBlock(settings.outlineThickness,
                   settings.dissolveAmount,
                   settings.verticalDissolve,
                   settings.outlineColor * settings.outlineIntensity);

        yield return new WaitForSeconds(settings.blinkDuration);

        ApplyBlock(0f, 0f, 0f, settings.outlineColor * settings.outlineIntensity);

        _blinkRoutine = null;
    }

    private void ApplyBlock(float outlineThickness, float dissolveAmount, float verticalDissolve, Color outlineColor)
    {
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            var sr = spriteRenderers[i];
            if (!sr) continue;

            sr.GetPropertyBlock(_mpb);
            _mpb.SetFloat(OutlineThicknessId, outlineThickness);
            _mpb.SetFloat(DisolveAmountId, dissolveAmount);
            _mpb.SetFloat(VerticalDisolveId, verticalDissolve);
            _mpb.SetColor(OutlineColorId, outlineColor);
            sr.SetPropertyBlock(_mpb);
        }
    }
}
