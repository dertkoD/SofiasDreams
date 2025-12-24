using UnityEngine;
using Zenject;

public class MinionInstaller : MonoInstaller
{
    [SerializeField] MinionConfig _config;
    [SerializeField] PlayerHealthConfig _healthConfig;
    [SerializeField] Health _health;
    [SerializeField] MinionBrain _brain;
    [SerializeField] MinionMotor2D _motor;
    [SerializeField] MinionShooter2D _shooter;
    [SerializeField] VisionCone2D _vision;

    public override void InstallBindings()
    {
        if (_config != null)
        {
            Container.BindInstance(_config).AsSingle();
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
        BindComponent(_shooter);
        BindComponent(_vision);

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
                Debug.LogError($"[MinionInstaller] Missing component binding for {typeof(T).Name} on {name}");
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
