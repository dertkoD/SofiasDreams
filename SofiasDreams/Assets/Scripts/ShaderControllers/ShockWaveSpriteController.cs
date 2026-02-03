using System.Collections;
using UnityEngine;
using Zenject;

public class ShockWaveSpriteController : MonoBehaviour
{
    [SerializeField] private float _shockWaveTime = 0.75f;
    [SerializeField] private float _waveDistanceEnd = 1f;
    [SerializeField] private Transform _ringSpawnPositionTransform;
    [SerializeField] private Camera _renderCamera;

    private Coroutine _shockWaveCoroutine;
    private Material _material;
    private SignalBus _bus;

    private static readonly int _waveDistanceFromCenter = Shader.PropertyToID("_WaveDistanceFromCenter");
    private static readonly int _ringSpawnPosition = Shader.PropertyToID("_RingSpawnPosition");

    [Inject]
    public void Inject(SignalBus bus)
    {
        _bus = bus;
    }

    private void Awake()
    {
        _material = GetComponent<SpriteRenderer>().material;
        
        if (_renderCamera == null)
            _renderCamera = Camera.main;
    }

    private void OnEnable()
    {
        if (_bus != null)
            _bus.Subscribe<HealStarted>(OnHealStarted);
        
        // Initialize to hidden state
        _material.SetFloat(_waveDistanceFromCenter, -0.1f);
    }

    private void OnDisable()
    {
        if (_bus != null)
            _bus.TryUnsubscribe<HealStarted>(OnHealStarted);
    }

    private void OnHealStarted(HealStarted signal)
    {
        CallShockWaveForward();
    }

    /// <summary>
    /// Forward animation: -0.1 → end value. Called on HealStarted.
    /// </summary>
    public void CallShockWaveForward()
    {
        UpdateRingSpawnPosition();
        
        if (_shockWaveCoroutine != null)
            StopCoroutine(_shockWaveCoroutine);
            
        _shockWaveCoroutine = StartCoroutine(ShockWaveAction(-0.1f, _waveDistanceEnd));
    }

    /// <summary>
    /// Reverse animation: end value → -0.1. Call from animation clip.
    /// </summary>
    public void CallShockWaveReverse()
    {
        if (_shockWaveCoroutine != null)
            StopCoroutine(_shockWaveCoroutine);
            
        _shockWaveCoroutine = StartCoroutine(ShockWaveAction(_waveDistanceEnd, -0.1f));
    }

    private IEnumerator ShockWaveAction(float startPos, float endPos)
    {
        _material.SetFloat(_waveDistanceFromCenter, startPos);

        float elapsedTime = 0f;

        while (elapsedTime < _shockWaveTime)
        {
            elapsedTime += Time.deltaTime;

            float lerpedAmount = Mathf.Lerp(startPos, endPos, elapsedTime / _shockWaveTime);
            _material.SetFloat(_waveDistanceFromCenter, lerpedAmount);

            yield return null;
        }
        
        _material.SetFloat(_waveDistanceFromCenter, endPos);
    }

    private void UpdateRingSpawnPosition()
    {
        if (_ringSpawnPositionTransform == null || _renderCamera == null)
            return;

        Vector3 viewportPos = _renderCamera.WorldToViewportPoint(_ringSpawnPositionTransform.position);
        _material.SetVector(_ringSpawnPosition, new Vector2(viewportPos.x, viewportPos.y));
    }
}
