using UnityEngine;
using Zenject;

public class EnemyFacade : MonoBehaviour
{
    [SerializeField] EnemyMovement _movement;
    [SerializeField] EnemyStateMachine _stateMachine;
    [SerializeField] EnemyPatrolController _patrolController;
    [SerializeField] Health _health;

    EnemyConfigSO _config;

    public EnemyMovement Movement => _movement;
    public EnemyStateMachine StateMachine => _stateMachine;
    public EnemyPatrolController PatrolController => _patrolController;
    public Health Health => _health;
    public EnemyConfigSO Config => _config;

    [Inject]
    public void Construct(EnemyConfigSO config, HealthSettings healthSettings)
    {
        _config = config;

        if (_movement != null)
            _movement.Configure(config);

        if (_patrolController != null)
            _patrolController.Configure(config);

        if (_health != null)
            _health.Configure(healthSettings);
    }
    
    public void SetPatrolPath(EnemyPatrolPath path)
    {
        _patrolController?.SetPath(path);
    }
    
    public void ApplyDamage(DamageInfo info)
    {
        _health?.ApplyDamage(info);
    }
}
