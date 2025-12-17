using UnityEngine;
using Zenject;
using Unity.Cinemachine;

[RequireComponent(typeof(CinemachineImpulseSource))]
public class CameraShakeService : MonoBehaviour
{
    CinemachineImpulseSource _impulseSource;

    CameraShakeConfig _config;
    SignalBus _bus;

    [Inject]
    public void Construct(CameraShakeConfig config, SignalBus bus)
    {
        _config = config;
        _bus = bus;
    }

    void Awake()
    {
        _impulseSource = GetComponent<CinemachineImpulseSource>();
    }

    void OnEnable()
    {
        _bus.Subscribe<AttackStarted>(OnAttackStarted);
        _bus.Subscribe<EnemyHit>(OnEnemyHit);
        _bus.Subscribe<TookDamage>(OnTookDamage);
    }

    void OnDisable()
    {
        _bus.Unsubscribe<AttackStarted>(OnAttackStarted);
        _bus.Unsubscribe<EnemyHit>(OnEnemyHit);
        _bus.Unsubscribe<TookDamage>(OnTookDamage);
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
    }

    void Shake(float force)
    {
        if (_impulseSource != null)
        {
            _impulseSource.GenerateImpulse(force);
        }
    }
}
