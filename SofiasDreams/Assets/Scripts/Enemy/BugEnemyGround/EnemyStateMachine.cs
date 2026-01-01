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
        if (EnemyCombatGate.IsBonfireSafe)
            return;
        
        _current?.Tick();
    }

    void OnHealthChanged()
    {
        if (_health == null || _health.IsAlive || _isDeadNotified)
            return;

        _isDeadNotified = true;
        ChangeState(_deadState);

        if (_bus == null) return;

        // ✅ Look for spawn meta on the whole hierarchy
        // Prefer facade if it exists, but fall back to this GO, parent, children.
        EnemySpawnMeta meta = null;

        if (_facade != null)
            meta = _facade.GetComponent<EnemySpawnMeta>();

        if (meta == null)
            meta = GetComponent<EnemySpawnMeta>();

        if (meta == null)
            meta = GetComponentInParent<EnemySpawnMeta>();

        if (meta == null)
            meta = GetComponentInChildren<EnemySpawnMeta>(true);

        if (meta != null)
        {
            //Debug.Log($"[PERSIST] Enemy died. meta.SpawnId='{meta.SpawnId}' meta.Mode={meta.RespawnMode} on '{gameObject.name}'");
            _bus.Fire(new EnemyKilledSignal
            {
                spawnId = meta.SpawnId,
                respawnMode = meta.RespawnMode
            });
        }
        else
        {
            //Debug.LogWarning($"[PERSIST] Enemy died but no EnemySpawnMeta found on '{gameObject.name}' (or facade).");
        }

        if (_facade != null)
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
