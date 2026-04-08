using UnityEngine;
using Zenject;

public class EnemyFacade : MonoBehaviour
{
    [SerializeField] EnemyMovement _movement;
    [SerializeField] EnemyStateMachine _stateMachine;
    [SerializeField] EnemyPatrolController _patrolController;
    [SerializeField] Health _health;
    [SerializeField] JumpingEnemyBrain _jumpingBrain;
    [SerializeField] WormBrain _wormBrain;
    [SerializeField] SwarmEnemyBrain _swarmBrain;
    [SerializeField] LazyMiniBossBrain _lazyMiniBossBrain;
    [SerializeField] LazyMiniBossGraphBridge _lazyMiniBossGraphBridge;

    EnemyConfigSO _config;

    public EnemyMovement Movement => _movement;
    public EnemyStateMachine StateMachine => _stateMachine;
    public EnemyPatrolController PatrolController => _patrolController;
    public Health Health => _health;
    public EnemyConfigSO Config => _config;

    [Inject]
    public void Construct([InjectOptional] EnemyConfigSO config, HealthSettings healthSettings)
    {
        _config = config;

        if (_movement != null && _config != null)
            _movement.Configure(_config);

        if (_patrolController != null && _config != null)
            _patrolController.Configure(_config);

        if (_health != null)
            _health.Configure(healthSettings);
    }
    
    public void SetPatrolPath(EnemyPatrolPath path)
    {
        _patrolController?.SetPath(path);
        
        if (_jumpingBrain) _jumpingBrain.PatrolPath = path;
        if (_wormBrain) _wormBrain.PatrolPath = path;
        if (_swarmBrain) _swarmBrain.SetPatrolPath(path);
        if (_lazyMiniBossBrain) _lazyMiniBossBrain.PatrolPath = path;
        if (_lazyMiniBossGraphBridge) _lazyMiniBossGraphBridge.SetPatrolPath(path);
    }
    
    public void ApplyDamage(DamageInfo info)
    {
        _health?.ApplyDamage(info);
    }
}
