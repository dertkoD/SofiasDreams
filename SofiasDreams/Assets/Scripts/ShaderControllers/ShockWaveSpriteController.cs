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
    [Tooltip("Starting value for WaveDistanceFromCenter (hidden state)")]
    [SerializeField] private float waveDistanceStart = -0.1f;
    
    [Tooltip("End value for WaveDistanceFromCenter (fully expanded)")]
    [SerializeField] private float waveDistanceEnd = 1.0f;
    
    [Tooltip("Duration of the forward shockwave animation in seconds")]
    [SerializeField] private float forwardAnimationDuration = 0.5f;
    
    [Tooltip("Duration of the reverse shockwave animation in seconds")]
    [SerializeField] private float reverseAnimationDuration = 0.3f;
    
    [Tooltip("Animation curve for the forward wave effect")]
    [SerializeField] private AnimationCurve forwardAnimationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Tooltip("Animation curve for the reverse wave effect")]
    [SerializeField] private AnimationCurve reverseAnimationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

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
            _bus.Subscribe<HealStarted>(OnHealStarted);
        }
        
        // Initialize shader to hidden state
        ResetShaderToInitialState();
    }

    private void OnDisable()
    {
        if (_bus != null)
        {
            _bus.TryUnsubscribe<HealStarted>(OnHealStarted);
        }
        
        StopCurrentAnimation();
    }

    private void OnHealStarted(HealStarted signal)
    {
        PlayShockWaveForward();
    }

    /// <summary>
    /// Plays the forward shockwave effect (from start to end value).
    /// Triggered automatically on HealStarted.
    /// </summary>
    public void PlayShockWaveForward()
    {
        if (shockWaveSpriteRenderer == null)
        {
            Debug.LogWarning("[ShockWaveSpriteController] SpriteRenderer is not assigned!");
            return;
        }

        StopCurrentAnimation();
        
        // Update ring spawn position before starting animation
        UpdateRingSpawnPosition();
        
        _animationCoroutine = StartCoroutine(AnimateWaveDistance(
            waveDistanceStart, 
            waveDistanceEnd, 
            forwardAnimationDuration, 
            forwardAnimationCurve));
    }

    /// <summary>
    /// Plays the reverse shockwave effect (from end value back to start).
    /// Call this method from animation clip/event to hide the wave.
    /// </summary>
    public void PlayShockWaveReverse()
    {
        if (shockWaveSpriteRenderer == null)
        {
            Debug.LogWarning("[ShockWaveSpriteController] SpriteRenderer is not assigned!");
            return;
        }

        StopCurrentAnimation();
        
        _animationCoroutine = StartCoroutine(AnimateWaveDistance(
            waveDistanceEnd, 
            waveDistanceStart, 
            reverseAnimationDuration, 
            reverseAnimationCurve));
    }

    /// <summary>
    /// Sets the WaveDistanceFromCenter value directly without animation.
    /// Useful for animation clips that need direct control.
    /// </summary>
    public void SetWaveDistance(float value)
    {
        if (shockWaveSpriteRenderer == null)
            return;

        shockWaveSpriteRenderer.GetPropertyBlock(_mpb);
        _mpb.SetFloat(WaveDistanceFromCenterId, value);
        shockWaveSpriteRenderer.SetPropertyBlock(_mpb);
    }

    /// <summary>
    /// Updates the RingSpawnPosition from the assigned transform.
    /// Call this if you need to update position during animation.
    /// </summary>
    public void UpdateRingSpawnPosition()
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

    /// <summary>
    /// Resets the shader to its initial hidden state.
    /// </summary>
    public void ResetShaderToInitialState()
    {
        if (shockWaveSpriteRenderer == null)
            return;

        shockWaveSpriteRenderer.GetPropertyBlock(_mpb);
        _mpb.SetFloat(WaveDistanceFromCenterId, waveDistanceStart);
        shockWaveSpriteRenderer.SetPropertyBlock(_mpb);
    }

    private void StopCurrentAnimation()
    {
        if (_animationCoroutine != null)
        {
            StopCoroutine(_animationCoroutine);
            _animationCoroutine = null;
        }
    }

    private IEnumerator AnimateWaveDistance(float from, float to, float duration, AnimationCurve curve)
    {
        float safeDuration = Mathf.Max(0.01f, duration);
        float elapsed = 0f;

        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / safeDuration);
            float curveValue = curve.Evaluate(normalizedTime);

            float waveDistance = Mathf.Lerp(from, to, curveValue);

            shockWaveSpriteRenderer.GetPropertyBlock(_mpb);
            _mpb.SetFloat(WaveDistanceFromCenterId, waveDistance);
            shockWaveSpriteRenderer.SetPropertyBlock(_mpb);

            yield return null;
        }

        // Ensure final value is set
        shockWaveSpriteRenderer.GetPropertyBlock(_mpb);
        _mpb.SetFloat(WaveDistanceFromCenterId, to);
        shockWaveSpriteRenderer.SetPropertyBlock(_mpb);

        _animationCoroutine = null;
    }

#if UNITY_EDITOR
    [ContextMenu("Test Forward")]
    private void TestForward()
    {
        if (Application.isPlaying)
            PlayShockWaveForward();
        else
            Debug.Log("[ShockWaveSpriteController] Test only works in Play mode.");
    }

    [ContextMenu("Test Reverse")]
    private void TestReverse()
    {
        if (Application.isPlaying)
            PlayShockWaveReverse();
        else
            Debug.Log("[ShockWaveSpriteController] Test only works in Play mode.");
    }
#endif
}
