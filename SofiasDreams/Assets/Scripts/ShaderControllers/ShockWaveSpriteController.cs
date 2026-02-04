using System.Collections;
using UnityEngine;
using Zenject;

/// <summary>
/// Controls ShockWave shader effect. Place on player with SpriteRenderer child that has the shader.
/// </summary>
public class ShockWaveSpriteController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer _shockWaveSpriteRenderer;
    [SerializeField] private Transform _spawnPointTransform;
    
    [Header("Timing")]
    [SerializeField] private float _shockWaveTime = 0.75f;
    [SerializeField] private float _shockWaveTimeReverse = 0.5f;
    [SerializeField] private float _shockWaveTimeInterrupted = 0.15f;
    
    [Header("Wave Settings")]
    [SerializeField] private float _waveDistanceEnd = 1f;

    private Coroutine _shockWaveCoroutine;
    private Material _material;
    private SignalBus _bus;

    private static readonly int _waveDistanceFromCenter = Shader.PropertyToID("_WaveDistanceFromCenter");
    private static readonly int _ringSpawnPositionId = Shader.PropertyToID("_RingSpawnPosition");

    [Inject]
    public void Inject(SignalBus bus)
    {
        _bus = bus;
    }

    private void Awake()
    {
        if (_shockWaveSpriteRenderer == null)
            _shockWaveSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        
        _material = _shockWaveSpriteRenderer.material;
    }

    private void OnEnable()
    {
        if (_bus != null)
        {
            _bus.Subscribe<HealStarted>(OnHealStarted);
            _bus.Subscribe<HealFinished>(OnHealFinished);
            _bus.Subscribe<HealInterrupted>(OnHealInterrupted);
        }
        
        // Initialize to hidden state
        _material.SetFloat(_waveDistanceFromCenter, -0.1f);
        UpdateRingSpawnPosition();
    }

    private void OnDisable()
    {
        if (_bus != null)
        {
            _bus.TryUnsubscribe<HealStarted>(OnHealStarted);
            _bus.TryUnsubscribe<HealFinished>(OnHealFinished);
            _bus.TryUnsubscribe<HealInterrupted>(OnHealInterrupted);
        }
    }

    private void OnHealStarted(HealStarted signal)
    {
        CallShockWaveForward();
    }

    private void OnHealFinished(HealFinished signal)
    {
        CallShockWaveReverse();
    }

    private void OnHealInterrupted(HealInterrupted signal)
    {
        CallShockWaveReverseQuick();
    }

    public void CallShockWaveForward()
    {
        UpdateRingSpawnPosition();
        
        if (_shockWaveCoroutine != null)
            StopCoroutine(_shockWaveCoroutine);
            
        _shockWaveCoroutine = StartCoroutine(ShockWaveAction(-0.1f, _waveDistanceEnd, _shockWaveTime));
    }

    public void CallShockWaveReverse()
    {
        if (_shockWaveCoroutine != null)
            StopCoroutine(_shockWaveCoroutine);
            
        _shockWaveCoroutine = StartCoroutine(ShockWaveAction(_waveDistanceEnd, -0.1f, _shockWaveTimeReverse));
    }

    public void CallShockWaveReverseQuick()
    {
        if (_shockWaveCoroutine != null)
            StopCoroutine(_shockWaveCoroutine);
        
        float currentValue = _material.GetFloat(_waveDistanceFromCenter);
        _shockWaveCoroutine = StartCoroutine(ShockWaveAction(currentValue, -0.1f, _shockWaveTimeInterrupted));
    }

    private IEnumerator ShockWaveAction(float startPos, float endPos, float duration)
    {
        _material.SetFloat(_waveDistanceFromCenter, startPos);

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;

            float lerpedAmount = Mathf.Lerp(startPos, endPos, elapsedTime / duration);
            _material.SetFloat(_waveDistanceFromCenter, lerpedAmount);

            yield return null;
        }
        
        _material.SetFloat(_waveDistanceFromCenter, endPos);
    }

    private void UpdateRingSpawnPosition()
    {
        if (_spawnPointTransform == null || _shockWaveSpriteRenderer == null)
        {
            // Default to center if no transform assigned
            _material.SetVector(_ringSpawnPositionId, new Vector2(0.5f, 0.5f));
            return;
        }

        // Convert spawn point world position to sprite's local UV coordinates
        Bounds bounds = _shockWaveSpriteRenderer.bounds;
        Vector3 spawnPos = _spawnPointTransform.position;

        // Normalize position within sprite bounds (0-1)
        float u = (spawnPos.x - bounds.min.x) / bounds.size.x;
        float v = (spawnPos.y - bounds.min.y) / bounds.size.y;

        _material.SetVector(_ringSpawnPositionId, new Vector2(u, v));
    }
}
