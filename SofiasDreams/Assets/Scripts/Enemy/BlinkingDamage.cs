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
    private static readonly int VerticalDisolveId   = Shader.PropertyToID("_VerticalDisolve");

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
            // Debug.Log($"[BlinkingDamage] Health changed: {_health.CurrentHP} (prev: {_lastHealth})");
            if (_health.CurrentHP < _lastHealth)
            {
                Blink();
            }
            _lastHealth = _health.CurrentHP;
        }
    }

    public void Blink()
    {
        if (!settings || !spriteRenderer) 
        {
            Debug.LogWarning("[BlinkingDamage] Missing settings or spriteRenderer!");
            return;
        }

        if (_health != null && !_health.IsAlive) return;

        if (_blinkRoutine != null) StopCoroutine(_blinkRoutine);
        _blinkRoutine = StartCoroutine(BlinkRoutine());
    }

    private IEnumerator BlinkRoutine()
    {
        spriteRenderer.GetPropertyBlock(_mpb);
        _mpb.SetFloat(OutlineThicknessId, settings.outlineThickness);
        _mpb.SetFloat(DisolveAmountId, settings.dissolveAmount);
        _mpb.SetFloat(VerticalDisolveId, settings.verticalDissolve);
        _mpb.SetColor(OutlineColorId, settings.outlineColor * settings.outlineIntensity);
        spriteRenderer.SetPropertyBlock(_mpb);

        yield return new WaitForSeconds(settings.blinkDuration);

        // Check if we are dead? Maybe better to not clear if another system takes over.
        // But for blink we want to restore.
        // If SpriteDissolveController is running, we might be fighting it?
        
        // Let's re-fetch the block to respect other changes if possible, 
        // though MPB replaces values for the renderer.
        spriteRenderer.GetPropertyBlock(_mpb);
        _mpb.SetFloat(OutlineThicknessId, 0f);
        _mpb.SetFloat(DisolveAmountId, 0f);
        _mpb.SetFloat(VerticalDisolveId, 0f);
        spriteRenderer.SetPropertyBlock(_mpb);

        _blinkRoutine = null;
    }
}
