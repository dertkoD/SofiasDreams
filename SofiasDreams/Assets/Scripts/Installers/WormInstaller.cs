using UnityEngine;
using Zenject;

public class WormInstaller : MonoInstaller
{
    [Header("Config")]
    [SerializeField] WormConfigSO _enemyConfig;
    [SerializeField] PlayerHealthConfig _healthConfig;

    [Header("Components")]
    [SerializeField] Health _health;
    [SerializeField] WormBrain _brain;
    [SerializeField] WormMotor2D _motor;
    [SerializeField] WormAnimatorAdapter _animator;
    [SerializeField] VisionCone2D _vision;

    public override void InstallBindings()
    {
        if (_enemyConfig == null)
            Debug.LogError($"[WormInstaller] Missing WormConfigSO on {name}");
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

        BindComponent(_health);
        BindComponent(_brain);
        BindComponent(_motor);
        BindComponent(_animator, optional: true);
        BindComponent(_vision, optional: true);

        // Bind standard components if they exist
        BindComponent(FindOptionalComponent<EnemyDamageFeedback>(), optional: true);
        BindComponent(FindOptionalComponent<Knockback2D>(), optional: true);
        BindComponent(FindOptionalComponent<LedgeGuard2D>(), optional: true);

        // Bind MobilityGate if used for stun logic
        Container.Bind<IMobilityGate>().To<MobilityGate>().AsSingle();
    }

    void BindComponent<T>(T component, bool optional = false) where T : class
    {
        if (component == null)
        {
            if (!optional)
                Debug.LogError($"[WormInstaller] Missing component binding for {typeof(T).Name} on {name}");
            return;
        }

        Container.BindInterfacesAndSelfTo<T>()
            .FromInstance(component)
            .AsSingle();
    }

    T FindOptionalComponent<T>() where T : Component
    {
        return GetComponentInChildren<T>(true);
    }
}
