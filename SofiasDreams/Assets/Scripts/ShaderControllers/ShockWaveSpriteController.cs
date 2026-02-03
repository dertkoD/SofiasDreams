using System.Collections;
using UnityEngine;
using Zenject;

/// <summary>
/// Controls the ShockWaveSprite shader effect when the player heals.
/// Animates the WaveDistanceFromCenter property and sets the RingSpawnPosition
/// based on the player's position.
/// </summary>
public class ShockWaveSpriteController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer shockWaveSpriteRenderer;
    [SerializeField] private Transform ringSpawnPositionTransform;
    [SerializeField] private Camera mainCamera;

    [Header("Animation Settings")]
    [Tooltip("Starting value for WaveDistanceFromCenter")]
    [SerializeField] private float waveDistanceStart = -0.1f;
    
    [Tooltip("End value for WaveDistanceFromCenter")]
    [SerializeField] private float waveDistanceEnd = 1.0f;
    
    [Tooltip("Duration of the shockwave animation in seconds")]
    [SerializeField] private float animationDuration = 0.5f;
    
    [Tooltip("Animation curve for the wave effect")]
    [SerializeField] private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private SignalBus _bus;
    private MaterialPropertyBlock _mpb;
    private Coroutine _animationCoroutine;

    private static readonly int WaveDistanceFromCenterId = Shader.PropertyToID("_WaveDistanceFromCenter");
    private static readonly int RingSpawnPositionId = Shader.PropertyToID("_RingSpawnPosition");

    [Inject]
    public void Inject(SignalBus bus)
    {
        _bus = bus;
    }

    private void Awake()
    {
        _mpb = new MaterialPropertyBlock();
        
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    private void OnEnable()
    {
        if (_bus != null)
        {
            _bus.Subscribe<HealFinished>(OnHealFinished);
        }
        
        // Initialize shader to hidden state
        ResetShaderToInitialState();
    }

    private void OnDisable()
    {
        if (_bus != null)
        {
            _bus.TryUnsubscribe<HealFinished>(OnHealFinished);
        }
        
        if (_animationCoroutine != null)
        {
            StopCoroutine(_animationCoroutine);
            _animationCoroutine = null;
        }
    }

    private void OnHealFinished(HealFinished signal)
    {
        PlayShockWave();
    }

    /// <summary>
    /// Plays the shockwave effect. Can be called externally for testing or other triggers.
    /// </summary>
    public void PlayShockWave()
    {
        if (shockWaveSpriteRenderer == null)
        {
            Debug.LogWarning("[ShockWaveSpriteController] SpriteRenderer is not assigned!");
            return;
        }

        if (_animationCoroutine != null)
        {
            StopCoroutine(_animationCoroutine);
        }

        _animationCoroutine = StartCoroutine(ShockWaveRoutine());
    }

    private IEnumerator ShockWaveRoutine()
    {
        // Update ring spawn position from transform
        UpdateRingSpawnPosition();

        float duration = Mathf.Max(0.01f, animationDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / duration);
            float curveValue = animationCurve.Evaluate(normalizedTime);

            float waveDistance = Mathf.Lerp(waveDistanceStart, waveDistanceEnd, curveValue);

            shockWaveSpriteRenderer.GetPropertyBlock(_mpb);
            _mpb.SetFloat(WaveDistanceFromCenterId, waveDistance);
            shockWaveSpriteRenderer.SetPropertyBlock(_mpb);

            yield return null;
        }

        // Ensure final value is set
        shockWaveSpriteRenderer.GetPropertyBlock(_mpb);
        _mpb.SetFloat(WaveDistanceFromCenterId, waveDistanceEnd);
        shockWaveSpriteRenderer.SetPropertyBlock(_mpb);

        _animationCoroutine = null;
        
        // Reset to initial state after animation completes
        ResetShaderToInitialState();
    }

    private void UpdateRingSpawnPosition()
    {
        if (ringSpawnPositionTransform == null || mainCamera == null || shockWaveSpriteRenderer == null)
            return;

        // Convert world position to viewport coordinates (0-1 range)
        Vector3 viewportPos = mainCamera.WorldToViewportPoint(ringSpawnPositionTransform.position);
        Vector2 uvPosition = new Vector2(viewportPos.x, viewportPos.y);

        shockWaveSpriteRenderer.GetPropertyBlock(_mpb);
        _mpb.SetVector(RingSpawnPositionId, uvPosition);
        shockWaveSpriteRenderer.SetPropertyBlock(_mpb);
    }

    private void ResetShaderToInitialState()
    {
        if (shockWaveSpriteRenderer == null)
            return;

        shockWaveSpriteRenderer.GetPropertyBlock(_mpb);
        _mpb.SetFloat(WaveDistanceFromCenterId, waveDistanceStart);
        shockWaveSpriteRenderer.SetPropertyBlock(_mpb);
    }

#if UNITY_EDITOR
    [ContextMenu("Test Shockwave")]
    private void TestShockwave()
    {
        if (Application.isPlaying)
        {
            PlayShockWave();
        }
        else
        {
            Debug.Log("[ShockWaveSpriteController] Test only works in Play mode.");
        }
    }
#endif
}
