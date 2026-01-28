using System.Collections;
using UnityEngine;

public class BlinkingDamage : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private BlinkingSettingsSO settings;

    private Health _health;
    private MaterialPropertyBlock _mpb;
    private Coroutine _blinkRoutine;
    private int _lastHealth;

    private static readonly int OutlineThicknessId  = Shader.PropertyToID("_OutlineThickness");
    private static readonly int OutlineColorId      = Shader.PropertyToID("_OutlineColor");
    private static readonly int DisolveAmountId     = Shader.PropertyToID("_DisolveAmount");

    private void Awake()
    {
        if (!spriteRenderer) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        _mpb = new MaterialPropertyBlock();

        _health = GetComponent<Health>();
        if (!_health) _health = GetComponentInParent<Health>();
    }

    private void Start()
    {
        if (_health)
        {
            _lastHealth = _health.CurrentHP;
            _health.OnHealthChanged += OnHealthChanged;
        }
    }

    private void OnDestroy()
    {
        if (_health)
        {
            _health.OnHealthChanged -= OnHealthChanged;
        }
    }

    private void OnHealthChanged()
    {
        if (_health)
        {
            if (_health.CurrentHP < _lastHealth)
            {
                Blink();
            }
            _lastHealth = _health.CurrentHP;
        }
    }

    public void Blink()
    {
        if (!settings || !spriteRenderer) return;

        if (_blinkRoutine != null) StopCoroutine(_blinkRoutine);
        _blinkRoutine = StartCoroutine(BlinkRoutine());
    }

    private IEnumerator BlinkRoutine()
    {
        spriteRenderer.GetPropertyBlock(_mpb);
        _mpb.SetFloat(OutlineThicknessId, settings.outlineThickness);
        _mpb.SetFloat(DisolveAmountId, settings.dissolveAmount);
        _mpb.SetColor(OutlineColorId, settings.outlineColor * settings.outlineIntensity);
        spriteRenderer.SetPropertyBlock(_mpb);

        yield return new WaitForSeconds(settings.blinkDuration);

        spriteRenderer.GetPropertyBlock(_mpb);
        _mpb.SetFloat(OutlineThicknessId, 0f);
        _mpb.SetFloat(DisolveAmountId, 0f);
        spriteRenderer.SetPropertyBlock(_mpb);

        _blinkRoutine = null;
    }
}
