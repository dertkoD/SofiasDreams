using System.Collections;
using UnityEngine;
using Zenject;
using Unity.Cinemachine;

public class ShockWaveSpriteController : MonoBehaviour
{
    [SerializeField] private float _shockWaveTime = 0.75f;
    [SerializeField] private float _shockWaveTimeReverse = 0.5f;
    [SerializeField] private float _shockWaveTimeInterrupted = 0.15f;
    [SerializeField] private float _waveDistanceEnd = 1f;

    private Transform _shockWaveSpawnPoint;
    private Coroutine _shockWaveCoroutine;
    private Material _material;
    private SignalBus _bus;
    private Camera _cinemachineOutputCamera;
    private CinemachineCamera _virtualCamera;

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
        
        FindCinemachineCamera();
    }

    private void FindCinemachineCamera()
    {
        // Find camera with CinemachineBrain (the actual rendering camera controlled by Cinemachine)
        var brain = FindFirstObjectByType<CinemachineBrain>();
        if (brain != null)
        {
            _cinemachineOutputCamera = brain.GetComponent<Camera>();
            
            // Get active virtual camera
            if (brain.ActiveVirtualCamera is CinemachineCamera vcam)
            {
                _virtualCamera = vcam;
            }
        }
        
        if (_cinemachineOutputCamera == null)
        {
            Debug.LogWarning("[ShockWaveSpriteController] CinemachineBrain not found, falling back to Camera.main");
            _cinemachineOutputCamera = Camera.main;
        }
    }

    private void OnEnable()
    {
        if (_bus != null)
        {
            _bus.Subscribe<PlayerSpawned>(OnPlayerSpawned);
            _bus.Subscribe<HealStarted>(OnHealStarted);
            _bus.Subscribe<HealFinished>(OnHealFinished);
            _bus.Subscribe<HealInterrupted>(OnHealInterrupted);
        }
        
        // Initialize to hidden state
        _material.SetFloat(_waveDistanceFromCenter, -0.1f);
    }

    private void OnDisable()
    {
        if (_bus != null)
        {
            _bus.TryUnsubscribe<PlayerSpawned>(OnPlayerSpawned);
            _bus.TryUnsubscribe<HealStarted>(OnHealStarted);
            _bus.TryUnsubscribe<HealFinished>(OnHealFinished);
            _bus.TryUnsubscribe<HealInterrupted>(OnHealInterrupted);
        }
    }

    private void OnPlayerSpawned(PlayerSpawned signal)
    {
        if (signal.facade != null)
        {
            // Use shockWaveSpawnPoint from PlayerFacade
            _shockWaveSpawnPoint = signal.facade.shockWaveSpawnPoint != null 
                ? signal.facade.shockWaveSpawnPoint 
                : signal.facade.transform;
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

    /// <summary>
    /// Forward animation: -0.1 → end value. Called on HealStarted.
    /// </summary>
    public void CallShockWaveForward()
    {
        UpdateRingSpawnPosition();
        
        if (_shockWaveCoroutine != null)
            StopCoroutine(_shockWaveCoroutine);
            
        _shockWaveCoroutine = StartCoroutine(ShockWaveAction(-0.1f, _waveDistanceEnd, _shockWaveTime));
    }

    /// <summary>
    /// Reverse animation: end value → -0.1. Called on HealFinished.
    /// </summary>
    public void CallShockWaveReverse()
    {
        UpdateRingSpawnPosition();
        
        if (_shockWaveCoroutine != null)
            StopCoroutine(_shockWaveCoroutine);
            
        _shockWaveCoroutine = StartCoroutine(ShockWaveAction(_waveDistanceEnd, -0.1f, _shockWaveTimeReverse));
    }

    /// <summary>
    /// Quick reverse animation: current value → -0.1. Called on HealInterrupted.
    /// </summary>
    public void CallShockWaveReverseQuick()
    {
        UpdateRingSpawnPosition();
        
        if (_shockWaveCoroutine != null)
            StopCoroutine(_shockWaveCoroutine);
        
        // Get current value and animate from there
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

            // Update position every frame to follow camera movement
            UpdateRingSpawnPosition();

            float lerpedAmount = Mathf.Lerp(startPos, endPos, elapsedTime / duration);
            _material.SetFloat(_waveDistanceFromCenter, lerpedAmount);

            yield return null;
        }
        
        _material.SetFloat(_waveDistanceFromCenter, endPos);
    }

    private void UpdateRingSpawnPosition()
    {
        if (_shockWaveSpawnPoint == null || _cinemachineOutputCamera == null)
            return;

        Vector2 viewportPos;

        // If we have virtual camera with follow target, calculate position relative to where camera SHOULD be
        if (_virtualCamera != null && _virtualCamera.Follow != null)
        {
            // Get the offset between spawn point and camera's follow target
            Vector3 followTargetPos = _virtualCamera.Follow.position;
            Vector3 spawnPointPos = _shockWaveSpawnPoint.position;
            
            // Calculate offset in world space
            Vector3 offset = spawnPointPos - followTargetPos;
            
            // Convert offset to viewport space (assuming orthographic or known FOV)
            // For orthographic camera:
            if (_cinemachineOutputCamera.orthographic)
            {
                float orthoSize = _cinemachineOutputCamera.orthographicSize;
                float aspect = _cinemachineOutputCamera.aspect;
                
                // Convert world offset to normalized viewport offset
                float viewportOffsetX = offset.x / (orthoSize * 2f * aspect);
                float viewportOffsetY = offset.y / (orthoSize * 2f);
                
                // Center (0.5, 0.5) + offset
                viewportPos = new Vector2(0.5f + viewportOffsetX, 0.5f + viewportOffsetY);
            }
            else
            {
                // Fallback for perspective camera
                viewportPos = _cinemachineOutputCamera.WorldToViewportPoint(spawnPointPos);
            }
        }
        else
        {
            // Fallback to standard conversion
            Vector3 vp = _cinemachineOutputCamera.WorldToViewportPoint(_shockWaveSpawnPoint.position);
            viewportPos = new Vector2(vp.x, vp.y);
        }

        _material.SetVector(_ringSpawnPosition, viewportPos);
    }
}
