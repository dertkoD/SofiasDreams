using UnityEngine;
using Zenject;

public class EnemyInstaller : MonoInstaller
{
    [Header("Config")]
    [SerializeField] EnemyConfigSO _enemyConfig;
    [SerializeField] PlayerHealthConfig _healthConfig; 

    [Header("Components")]
    [SerializeField] EnemyFacade _facade;

    public override void InstallBindings()
    {
        Container.BindInstance(_enemyConfig).AsSingle();

        HealthSettings healthSettings = new HealthSettings
        {
            maxHP      = _healthConfig.maxHP,
            invulnTime = _healthConfig.invulnTime
        };

        Container.BindInstance(healthSettings).AsSingle();

        BindComponent(_facade);
        BindComponent(_facade != null ? _facade.Health : null);
        BindComponent(_facade != null ? _facade.Movement : null, optional: true);
        BindComponent(_facade != null ? _facade.PatrolController : null, optional: true);
        BindComponent(FindOptionalComponent<EnemyDamageFeedback>(), optional: true);
        BindComponent(FindOptionalComponent<Knockback2D>(), optional: true);

        Container.Bind<IMobilityGate>()
            .To<MobilityGate>()
            .AsSingle();
    }

    void BindComponent<T>(T component, bool optional = false) where T : class
    {
        if (component == null)
        {
            if (!optional)
                Debug.LogError($"[EnemyInstaller] Missing component binding for {typeof(T).Name} on {name}");
            return;
        }

        Container.BindInterfacesAndSelfTo<T>()
            .FromInstance(component)
            .AsSingle();
    }

    T FindOptionalComponent<T>() where T : Component
    {
        if (_facade == null)
            return null;

        return _facade.GetComponentInChildren<T>(true);
    }
}