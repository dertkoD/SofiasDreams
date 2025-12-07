using UnityEngine;
using Zenject;

public class EnemyStateMachine : MonoBehaviour
{
    [SerializeField] EnemyMovement _movement;
    [SerializeField] EnemyPatrolController _patrol;
    [SerializeField] Health _health;
    [SerializeField] EnemyFacade _facade;

    SignalBus _bus;

    IEnemyState _current;
    EnemyPatrolState _patrolState;
    EnemyDeadState _deadState;

    bool _isDeadNotified;

    [Inject]
    public void Construct(SignalBus bus)
    {
        _bus = bus;
    }

    void Awake()
    {
        _patrolState = new EnemyPatrolState(_patrol);
        _deadState = new EnemyDeadState(_movement);
    }

    void OnEnable()
    {
        if (_health != null)
            _health.OnHealthChanged += OnHealthChanged;
    }

    void OnDisable()
    {
        if (_health != null)
            _health.OnHealthChanged -= OnHealthChanged;
    }

    void Start()
    {
        ChangeState(_patrolState);
    }

    void Update()
    {
        _current?.Tick();
    }

    void OnHealthChanged()
    {
        if (_health == null || _health.IsAlive || _isDeadNotified)
            return;

        _isDeadNotified = true;
        ChangeState(_deadState);

        if (_bus != null && _facade != null)
            _bus.Fire(new EnemyDiedSignal(_facade));
    }

    void ChangeState(IEnemyState next)
    {
        if (_current == next || next == null)
            return;

        _current?.Exit();
        _current = next;
        _current.Enter();
    }
}
