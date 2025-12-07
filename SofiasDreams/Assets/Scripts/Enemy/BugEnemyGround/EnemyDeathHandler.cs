using UnityEngine;
using Zenject;

public class EnemyDeathHandler : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] Health _health;
    [SerializeField] EnemyMovement _movement;
    [SerializeField] EnemyFacade _facade;
    [SerializeField] Rigidbody2D _rb;
    [SerializeField] Collider2D[] _colliders;

    [Header("Behaviour")]
    [SerializeField, Min(0f)] float _destroyDelay = 1.0f;

    SignalBus _bus;
    bool _handled;

    [Inject]
    public void Construct(SignalBus bus)
    {
        _bus = bus;
    }

    void Awake()
    {
        if (_health == null)      _health    = GetComponent<Health>();
        if (_movement == null)    _movement  = GetComponent<EnemyMovement>();
        if (_facade == null)      _facade    = GetComponent<EnemyFacade>();
        if (_rb == null)          _rb        = GetComponent<Rigidbody2D>();
        if (_colliders == null || _colliders.Length == 0)
            _colliders = GetComponentsInChildren<Collider2D>(true);
    }

    void Update()
    {
        if (_handled || _health == null)
            return;

        if (!_health.IsAlive)
            HandleDeath();
    }

    void HandleDeath()
    {
        _handled = true;

        if (_movement != null)
            _movement.Stop();

        if (_rb != null)
        {
            _rb.linearVelocity   = Vector2.zero;
            _rb.simulated  = false;
        }

        if (_colliders != null)
        {
            foreach (var c in _colliders)
                if (c) c.enabled = false;
        }

        if (_facade != null && _bus != null)
            _bus.Fire(new EnemyDiedSignal(_facade));

        Destroy(gameObject, _destroyDelay);
    }
}
