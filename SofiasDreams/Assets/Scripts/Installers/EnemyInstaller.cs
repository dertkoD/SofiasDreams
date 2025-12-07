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

        Container.BindInstance(_facade).AsSingle();

        Container.Bind<IMobilityGate>()
            .To<MobilityGate>()
            .AsSingle();
    }
}