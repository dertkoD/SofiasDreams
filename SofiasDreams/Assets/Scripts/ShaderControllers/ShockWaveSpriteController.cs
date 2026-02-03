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
    
    [Tooltip("Main rendering camera (with CinemachineBrain). If empty, will use Camera.main")]
    [SerializeField] private Camera renderCamera;

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

    [Header("Debug")]
    [Tooltip("Enable to see debug logs with position values")]
    [SerializeField] private bool debugMode = false;
    
    [Tooltip("Manually set position (0-1 viewport coords). Use if auto-calculation doesn't work")]
    [SerializeField] private bool useManualPosition = false;
    [SerializeField] private Vector2 manualRingPosition = new Vector2(0.5f, 0.5f);

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
        
        if (renderCamera == null)
            renderCamera = Camera.main;
        
        if (renderCamera == null)
            Debug.LogWarning("[ShockWaveSpriteController] No camera found! Assign renderCamera in inspector.");
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
        if (shockWaveSpriteRenderer == null)
            return;

        Vector2 uvPosition;

        if (useManualPosition)
        {
            uvPosition = manualRingPosition;
        }
        else
        {
            if (ringSpawnPositionTransform == null || renderCamera == null)
            {
                if (debugMode)
                    Debug.LogWarning("[ShockWaveSpriteController] Missing transform or camera reference!");
                return;
            }

            // Convert world position to viewport coordinates (0-1 range)
            // Viewport: (0,0) = bottom-left, (1,1) = top-right, (0.5, 0.5) = center
            Vector3 viewportPos = renderCamera.WorldToViewportPoint(ringSpawnPositionTransform.position);
            uvPosition = new Vector2(viewportPos.x, viewportPos.y);
        }

        if (debugMode)
        {
            Debug.Log($"[ShockWaveSpriteController] RingSpawnPosition set to: {uvPosition}");
            if (ringSpawnPositionTransform != null)
                Debug.Log($"[ShockWaveSpriteController] World pos: {ringSpawnPositionTransform.position}");
        }

        shockWaveSpriteRenderer.GetPropertyBlock(_mpb);
        _mpb.SetVector(RingSpawnPositionId, uvPosition);
        shockWaveSpriteRenderer.SetPropertyBlock(_mpb);
    }

    /// <summary>
    /// Manually set the ring spawn position in viewport coordinates (0-1).
    /// (0,0) = bottom-left, (1,1) = top-right, (0.5, 0.5) = center
    /// </summary>
    public void SetRingSpawnPosition(Vector2 viewportPosition)
    {
        if (shockWaveSpriteRenderer == null)
            return;

        shockWaveSpriteRenderer.GetPropertyBlock(_mpb);
        _mpb.SetVector(RingSpawnPositionId, viewportPosition);
        shockWaveSpriteRenderer.SetPropertyBlock(_mpb);
        
        if (debugMode)
            Debug.Log($"[ShockWaveSpriteController] Manual RingSpawnPosition: {viewportPosition}");
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

    [ContextMenu("Debug: Log Current Position")]
    private void DebugLogPosition()
    {
        if (ringSpawnPositionTransform != null && renderCamera != null)
        {
            Vector3 viewportPos = renderCamera.WorldToViewportPoint(ringSpawnPositionTransform.position);
            Debug.Log($"[ShockWaveSpriteController] Transform world pos: {ringSpawnPositionTransform.position}");
            Debug.Log($"[ShockWaveSpriteController] Viewport position: ({viewportPos.x:F3}, {viewportPos.y:F3})");
        }
        else
        {
            Debug.LogWarning("[ShockWaveSpriteController] Missing references!");
        }
    }

    [ContextMenu("Debug: Set Position to Center")]
    private void DebugSetCenter()
    {
        if (Application.isPlaying && shockWaveSpriteRenderer != null)
        {
            _mpb ??= new MaterialPropertyBlock();
            shockWaveSpriteRenderer.GetPropertyBlock(_mpb);
            _mpb.SetVector(RingSpawnPositionId, new Vector2(0.5f, 0.5f));
            shockWaveSpriteRenderer.SetPropertyBlock(_mpb);
            Debug.Log("[ShockWaveSpriteController] Position set to center (0.5, 0.5)");
        }
    }
#endif
}
