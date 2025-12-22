using System.Collections;
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
    IEnemyGroundChecker _groundChecker;

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
            
        _groundChecker = GetComponent<IEnemyGroundChecker>();
    }

    void OnEnable()
    {
        if (_health != null)
            _health.OnHealthChanged += OnHealthChanged;

        EvaluateDeath();
    }

    void OnDisable()
    {
        if (_health != null)
            _health.OnHealthChanged -= OnHealthChanged;
    }

    void OnHealthChanged()
    {
        EvaluateDeath();
    }

    void EvaluateDeath()
    {
        if (_handled || _health == null || _health.IsAlive)
            return;

        HandleDeath();
    }

    void HandleDeath()
    {
        _handled = true;

        // 1. Fire Logic Signal
        if (_facade != null && _bus != null)
        {
            bool killedByPlayer = false;
            if (_health != null && _health.LastHit != null && _health.LastHit.source != null)
            {
                bool isPlayer = _health.LastHit.source.GetComponentInParent<Weapon>() != null ||
                                _health.LastHit.source.GetComponentInParent<Grappler2D>() != null;

                if (!isPlayer && _health.LastHit.source.CompareTag("Weapon"))
                    isPlayer = true;

                if (!isPlayer && _health.LastHit.source.transform.root.name.Contains("Player"))
                    isPlayer = true;

                if (isPlayer)
                {
                    killedByPlayer = true;
                } 
            }

            _bus.Fire(new EnemyDiedSignal(_facade, killedByPlayer));
        }
        
        // 2. Stop active movement logic
        if (_movement != null)
            _movement.Stop();

        // 3. Determine fall behavior
        var mode = _movement != null ? _movement.MovementMode : EnemyMovementMode.GroundOnly;
        
        if (mode == EnemyMovementMode.Planar2D)
        {
            // Flying enemies die in place (or per existing logic)
            FinalizeDeath();
        }
        else
        {
            // Ground/Jumping/Worm enemies should fall to ground
            StartCoroutine(FallAndDieRoutine());
        }
    }
    
    IEnumerator FallAndDieRoutine()
    {
        // Ensure physics is active so it falls
        if (_rb != null)
        {
            _rb.simulated = true;
            // Note: _movement.Stop() might have zeroed velocity. Gravity should take over.
        }

        // Wait until grounded
        if (_groundChecker != null)
        {
            // Wait at least one frame to let physics run
            yield return new WaitForFixedUpdate();
            
            float timeout = 5f; // Prevent hanging forever if off-map
            float timer = 0f;
            
            while (!_groundChecker.IsGrounded && timer < timeout)
            {
                timer += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }
        }
        else
        {
            // Fallback: wait a bit for gravity to act if we can't check ground
             yield return new WaitForSeconds(0.5f);
        }

        FinalizeDeath();
    }

    void FinalizeDeath()
    {
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

        Destroy(gameObject, _destroyDelay);
    }
}
