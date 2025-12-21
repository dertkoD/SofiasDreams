using UnityEngine;
using Zenject;

public class WormInstaller : MonoInstaller
{
    [SerializeField] WormConfigSO _config;

    public override void InstallBindings()
    {
        if (_config)
        {
            Container.BindInstance(_config);
            
            Container.BindInstance(new HealthSettings 
            { 
                maxHP = _config.maxHP, 
                invulnTime = _config.invulnTime 
            });
        }

        // Bind IHealth to the Health component on this GameObject/Hierarchy
        Container.Bind<IHealth>().To<Health>().FromComponentInHierarchy().AsSingle();
    }
}
