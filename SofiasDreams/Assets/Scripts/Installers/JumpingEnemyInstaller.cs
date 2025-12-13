using UnityEngine;
using Zenject;

public class JumpingEnemyInstaller : MonoInstaller
{
    [Header("Config")]
    [SerializeField] JumpingEnemyConfigSO _enemyConfig;
    [SerializeField] PlayerHealthConfig _healthConfig;

    [Header("Components")]
    [SerializeField] Health _health;
    [SerializeField] JumpingEnemyBrain _brain;
    [SerializeField] JumpingEnemyMotor2D _motor;
    [SerializeField] JumpingEnemyGroundChecker2D _groundChecker;
    [SerializeField] JumpingEnemyAnimatorAdapter _animator;
    [SerializeField] VisionCone2D _vision;

    public override void InstallBindings()
    {
        if (_enemyConfig == null)
            Debug.LogError($"[JumpingEnemyInstaller] Missing JumpingEnemyConfigSO on {name}");
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
            Debug.LogError($"[JumpingEnemyInstaller] Missing PlayerHealthConfig on {name}");
        }

        BindComponent(_health);
        BindComponent(_brain);
        BindComponent(_motor);
        BindComponent(_groundChecker, optional: true);
        BindComponent(_animator, optional: true);
        BindComponent(_vision, optional: true);

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
                Debug.LogError($"[JumpingEnemyInstaller] Missing component binding for {typeof(T).Name} on {name}");
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

