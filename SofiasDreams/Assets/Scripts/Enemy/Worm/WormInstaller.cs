using UnityEngine;
using Zenject;

public class WormInstaller : MonoInstaller
{
    [SerializeField] WormConfigSO _config;
    [SerializeField] HealthSettings _healthSettings;

    public override void InstallBindings()
    {
        if (_config)
        {
            Container.BindInstance(_config);
        }
        
        Container.BindInstance(_healthSettings);

        // Bind IHealth to the Health component on this GameObject/Hierarchy
        Container.Bind<IHealth>().To<Health>().FromComponentInHierarchy().AsSingle();
    }
}
