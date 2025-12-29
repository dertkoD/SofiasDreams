using UnityEngine;
using Zenject;

public class LazyMiniBossInstaller : MonoInstaller
{
    [Header("Config")]
    [SerializeField] LazyMiniBossConfigSO _enemyConfig;
    [SerializeField] PlayerHealthConfig _healthConfig;

    [Header("Components")]
    [SerializeField] Health _health;
    [SerializeField] LazyMiniBossBrain _brain;
    [SerializeField] LazyMiniBossMotor2D _motor;
    [SerializeField] LazyMiniBossAnimatorAdapter _animator;
    [SerializeField] VisionCone2D _vision;

    public override void InstallBindings()
    {
        if (_enemyConfig == null)
            Debug.LogError($"[LazyMiniBossInstaller] Missing LazyMiniBossConfigSO on {name}");
        else
            Container.BindInstance(_enemyConfig).AsSingle();

        if (_healthConfig != null)
        {
            HealthSettings healthSettings = new HealthSettings
            {
                maxHP = _healthConfig.maxHP,
                invulnTime = _healthConfig.invulnTime
            };
            Container.BindInstance(healthSettings).AsSingle();
            
            if (_health != null)
                _health.Configure(healthSettings);
        }
        else
        {
            Debug.LogError($"[LazyMiniBossInstaller] Missing HealthConfig on {name}");
        }

        BindComponent(_health);
        BindComponent(_brain);
        BindComponent(_motor);
        BindComponent(_animator);
        BindComponent(_vision, optional: true);

        // Bind common components if they exist on the prefab
        BindComponent(GetComponentInChildren<EnemyDamageFeedback>(true), optional: true);
        BindComponent(GetComponentInChildren<Knockback2D>(true), optional: true);
        BindComponent(GetComponentInChildren<EnemyContactDamage>(true), optional: true);

        Container.Bind<IMobilityGate>().To<MobilityGate>().AsSingle();
    }

    void BindComponent<T>(T component, bool optional = false) where T : class
    {
        if (component == null)
        {
            if (!optional)
                Debug.LogError($"[LazyMiniBossInstaller] Missing component binding for {typeof(T).Name} on {name}");
            return;
        }

        Container.BindInterfacesAndSelfTo<T>()
            .FromInstance(component)
            .AsSingle();
    }
}
