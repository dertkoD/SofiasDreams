using UnityEngine;
using Zenject;

public class SwarmEnemyInstaller : MonoInstaller
{
    [Header("Config")]
    [SerializeField] SwarmConfig _enemyConfig;
    [SerializeField] PlayerHealthConfig _healthConfig;

    [Header("Components")]
    [SerializeField] Health _health;
    [SerializeField] SwarmEnemyBrain _brain;
    [SerializeField] SwarmEnemyMotor2D _motor;
    [SerializeField] SwarmMinionSpawner _spawner;
    [SerializeField] VisionCone2D _vision;
    [SerializeField] EnemyFacade _facade;

    public override void InstallBindings()
    {
        if (_enemyConfig == null)
        {
            Debug.LogError($"[SwarmEnemyInstaller] Missing SwarmConfig on {name}");
        }
        else
        {
            Container.BindInstance(_enemyConfig).AsSingle();
        }

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
        BindComponent(_spawner);
        BindComponent(_vision);
        BindComponent(_facade);

        BindComponent(FindOptionalComponent<EnemyDamageFeedback>(), optional: true);
        BindComponent(FindOptionalComponent<Knockback2D>(), optional: true);
        BindComponent(FindOptionalComponent<EnemyDeathHandler>(), optional: true);
        BindComponent(FindOptionalComponent<EnemyContactDamage>(), optional: true);
    }

    void BindComponent<T>(T component, bool optional = false) where T : class
    {
        if (component == null)
        {
            if (!optional)
                Debug.LogError($"[SwarmEnemyInstaller] Missing component binding for {typeof(T).Name} on {name}");
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
