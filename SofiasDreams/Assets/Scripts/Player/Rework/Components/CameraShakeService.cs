using UnityEngine;
using Zenject;
using Unity.Cinemachine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

[RequireComponent(typeof(CinemachineImpulseSource))]
public class CameraShakeService : MonoBehaviour
{
    CinemachineImpulseSource _impulseSource;

    CameraShakeConfig _config;
    SignalBus _bus;
    
    Volume _volume;
    Vignette _vignette;
    
    Coroutine _healShakeCo;
    Coroutine _dashShakeCo;
    Coroutine _vignetteCo;

    [Inject]
    public void Construct(CameraShakeConfig config, SignalBus bus)
    {
        _config = config;
        _bus = bus;
    }

    void Awake()
    {
        _impulseSource = GetComponent<CinemachineImpulseSource>();
        
        // Find Global Volume
        var volumes = FindObjectsByType<Volume>(FindObjectsSortMode.None);
        foreach (var v in volumes)
        {
            if (v.isGlobal)
            {
                _volume = v;
                break;
            }
        }

        if (_volume != null)
        {
            if (!_volume.profile.TryGet(out _vignette))
            {
                Debug.LogWarning("[CameraShakeService] Vignette override not found in Global Volume profile.");
            }
        }
        else
        {
            Debug.LogWarning("[CameraShakeService] Global Volume not found.");
        }
    }

    void OnEnable()
    {
        _bus.Subscribe<AttackStarted>(OnAttackStarted);
        _bus.Subscribe<EnemyHit>(OnEnemyHit);
        _bus.Subscribe<TookDamage>(OnTookDamage);
        
        _bus.Subscribe<HealStarted>(OnHealStarted);
        _bus.Subscribe<HealFinished>(OnHealFinished);
        _bus.Subscribe<HealInterrupted>(OnHealInterrupted);
        
        _bus.Subscribe<DashStarted>(OnDashStarted);
        _bus.Subscribe<DashFinished>(OnDashFinished);
    }

    void OnDisable()
    {
        _bus.Unsubscribe<AttackStarted>(OnAttackStarted);
        _bus.Unsubscribe<EnemyHit>(OnEnemyHit);
        _bus.Unsubscribe<TookDamage>(OnTookDamage);
        
        _bus.Unsubscribe<HealStarted>(OnHealStarted);
        _bus.Unsubscribe<HealFinished>(OnHealFinished);
        _bus.Unsubscribe<HealInterrupted>(OnHealInterrupted);
        
        _bus.Unsubscribe<DashStarted>(OnDashStarted);
        _bus.Unsubscribe<DashFinished>(OnDashFinished);
    }

    void OnAttackStarted()
    {
        Shake(_config.airAttackForce);
    }

    void OnEnemyHit()
    {
        Shake(_config.enemyHitForce);
    }

    void OnTookDamage(TookDamage signal)
    {
         Shake(_config.damageTakenForce);
         PlayVignette();
    }
    
    void OnHealStarted()
    {
        StopHealShake();
        _healShakeCo = StartCoroutine(ContinuousShakeRoutine(_config.healShakeForce));
    }
    
    void OnHealFinished() => StopHealShake();
    void OnHealInterrupted() => StopHealShake();
    
    void StopHealShake()
    {
        if (_healShakeCo != null)
        {
            StopCoroutine(_healShakeCo);
            _healShakeCo = null;
        }
    }
    
    void OnDashStarted(DashStarted signal)
    {
        StopDashShake();
        _dashShakeCo = StartCoroutine(ContinuousShakeRoutine(_config.dashShakeForce));
    }
    
    void OnDashFinished(DashFinished signal) => StopDashShake();
    
    void StopDashShake()
    {
        if (_dashShakeCo != null)
        {
            StopCoroutine(_dashShakeCo);
            _dashShakeCo = null;
        }
    }

    IEnumerator ContinuousShakeRoutine(float force)
    {
        while (true)
        {
            Shake(force);
            yield return new WaitForSeconds(_config.continuousShakeFrequency > 0 ? _config.continuousShakeFrequency : 0.05f);
        }
    }

    void Shake(float force)
    {
        if (_impulseSource != null)
        {
            // Генерируем случайный вектор направления для тряски (X, Y)
            // Это создаст более живое ощущение "тряски", чем просто удар в одну сторону
            Vector3 randomDirection = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f).normalized;
            
            // Передаем вектор скорости. Это переопределит Default Velocity в инспекторе,
            // но сохранит форму кривой (Impulse Shape) и длительность.
            _impulseSource.GenerateImpulse(randomDirection * force);
        }
    }
    
    void PlayVignette()
    {
        if (_vignette == null)
        {
             // Try to find it again, maybe it was added later or lost reference?
             // Or just log error if we really expect it.
             Debug.LogError("[CameraShakeService] Cannot play vignette: _vignette is null!");
             return;
        }

        if (_vignetteCo != null) StopCoroutine(_vignetteCo);
        _vignetteCo = StartCoroutine(VignetteRoutine());
    }
    
    IEnumerator VignetteRoutine()
    {
        _vignette.active = true;
        
        // Force reset to ensure we start from clean state
        float targetIntensity = _config.vignetteIntensity;
        
        // Override color
        _vignette.color.Override(_config.vignetteColor);
        
        float t = 0;
        float halfDuration = _config.vignetteDuration * 0.5f;
        
        // Fade In
        while (t < halfDuration)
        {
            t += Time.deltaTime;
            float progress = t / halfDuration;
            // Use Override to set value
            _vignette.intensity.Override(Mathf.Lerp(0f, targetIntensity, progress));
            yield return null;
        }
        
        t = 0;
        // Fade Out
        while (t < halfDuration)
        {
            t += Time.deltaTime;
            float progress = t / halfDuration;
            _vignette.intensity.Override(Mathf.Lerp(targetIntensity, 0f, progress));
            yield return null;
        }
        
        _vignette.intensity.Override(0f);
        _vignette.active = false;
    }
}
