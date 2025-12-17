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
            // Генерируем случайный вектор направления для тряски (X, Y)
            // Это создаст более живое ощущение "тряски", чем просто удар в одну сторону
            Vector3 randomDirection = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f).normalized;
            
            // Передаем вектор скорости. Это переопределит Default Velocity в инспекторе,
            // но сохранит форму кривой (Impulse Shape) и длительность.
            _impulseSource.GenerateImpulse(randomDirection * force);
        }
    }
}
