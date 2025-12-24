using UnityEngine;
using Zenject;

public class SwarmEnemyBrain : MonoBehaviour
{
    enum State
    {
        Patrol,
        Aggro,
        Evasion,
        ReturnToPatrol,
        Dead
    }

    [Header("Refs")] [SerializeField] SwarmEnemyMotor2D _motor;
    [SerializeField] SwarmMinionSpawner _spawner;
    [SerializeField] VisionCone2D _vision;
    [SerializeField] EnemyPatrolPath _patrolPath;
    [SerializeField] Animator _animator;
    [SerializeField] Health _health;

    SwarmConfig _config;
    Transform _player;
    State _state;

    // Patrol
    int _pathIndex;
    int _pathDir = 1;

    // Aggro
    float _forgetTimer;
    bool _hasSeenPlayer;
    Vector2 _lastSeenPos;

    // Evasion
    Vector2 _fleeTarget;

    [Inject]
    public void Construct(SwarmConfig config)
    {
        _config = config;
    }

    void Awake()
    {
        if (!_motor) _motor = GetComponent<SwarmEnemyMotor2D>();
        if (!_spawner) _spawner = GetComponent<SwarmMinionSpawner>();
        if (!_vision) _vision = GetComponentInChildren<VisionCone2D>();
        if (!_animator) _animator = GetComponentInChildren<Animator>();
        if (!_health) _health = GetComponent<Health>();
        if (!_patrolPath) _patrolPath = GetComponentInChildren<EnemyPatrolPath>(); // Try local first

        if (_patrolPath == null)
            _patrolPath = FindNearestPatrolPath();

        _state = State.Patrol;
    }

    void Start()
    {
        // Try to find player if not set
        if (_player == null)
        {
            var pf = FindObjectOfType<PlayerFacade>();
            if (pf != null) _player = pf.transform;
        }

        if (_patrolPath != null && _patrolPath.Count > 0)
        {
            _pathIndex = FindNearestWaypointIndex(transform.position);
        }
    }

    void OnEnable()
    {
        if (_health) _health.OnHealthChanged += OnHealthChanged;
    }

    void OnDisable()
    {
        if (_health) _health.OnHealthChanged -= OnHealthChanged;
    }

    void OnHealthChanged()
    {
        if (_health == null) return;
        if (!_health.IsAlive)
        {
            OnDeath();
            return;
        }

        if (_health.LastHit != null && _health.LastHit.source != null)
        {
            // If attacked by player (or anything), treat as seeing player
            if (_player == null || _player != _health.LastHit.source)
            {
                _player = _health.LastHit.source;
            }
            
            _forgetTimer = _config.aggroForgetSeconds;
            // Trigger Evasion immediately
        }

        OnDamageTaken();
    }

    void Update()
    {
        if (_state == State.Dead) return;

        bool seesPlayer = TrySense(out var target);
        if (seesPlayer)
        {
            _player = target;
            _lastSeenPos = target.position;
            _hasSeenPlayer = true;
            _forgetTimer = _config.aggroForgetSeconds;

            if (_state != State.Aggro && _state != State.Evasion)
            {
                EnterAggro();
            }
        }
        else if (_state == State.Aggro || _state == State.Evasion)
        {
            _forgetTimer -= Time.deltaTime;
            if (_forgetTimer <= 0f)
            {
                EnterReturnToPatrol();
            }
        }

        switch (_state)
        {
            case State.Patrol:
                TickPatrol();
                break;
            case State.Aggro:
                TickAggroBehavior();
                break;
            case State.Evasion:
                TickAggroBehavior();
                break;
            case State.ReturnToPatrol:
                TickReturnToPatrol();
                break;
        }

        UpdateAnimator();
    }

    void TickPatrol()
    {
        if (_patrolPath == null || _patrolPath.Count == 0) return;

        Vector2 target = _patrolPath.GetPoint(_pathIndex);
        _motor.MoveTo(target, _config.patrolSpeed);

        if (Vector2.Distance(transform.position, target) <= _config.waypointArriveDistance)
        {
            AdvancePathIndex();
        }
    }

    // Old separate ticks removed in favor of combined TickAggroBehavior

    void TickReturnToPatrol()
    {
        if (_patrolPath == null)
        {
            _state = State.Patrol; // Just switch state and idle
            return;
        }

        Vector2 target = _patrolPath.GetPoint(_pathIndex);
        _motor.MoveTo(target, _config.patrolSpeed);

        if (Vector2.Distance(transform.position, target) <= _config.waypointArriveDistance)
        {
            _state = State.Patrol;
        }
    }

    void EnterAggro()
    {
        _state = State.Aggro;
        if (_animator) _animator.SetTrigger("Angry");
        if (_spawner) _spawner.EnableSpawning(true);
    }

    // Combined Aggro/Evasion behavior
    void TickAggroBehavior()
    {
        if (_player == null) return;

        float dist = Vector2.Distance(transform.position, _player.position);

        // Flee/Maintain Distance Logic
        // User wants enemy to move away if it sees the player.
        // We use maintainDistance as the threshold.
        if (dist < _config.maintainDistance)
        {
            _state = State.Evasion;

            // Flee away
            Vector2 dir = (transform.position - _player.position).normalized;
            // Calculate a point further away
            Vector2 fleePos = (Vector2)transform.position + dir * 5.0f;
            _motor.MoveTo(fleePos, _config.fleeSpeed);
        }
        else
        {
            _state = State.Aggro;
            // Stop and chill, let minions work
            _motor.Stop();
        }

        // Spawning Logic (Always active in Aggro/Evasion)
        if (_spawner) _spawner.SetAggroTarget(_player);
    }

    void EnterReturnToPatrol()
    {
        _state = State.ReturnToPatrol;
        if (_animator) _animator.SetTrigger("Idle");
        if (_spawner) _spawner.EnableSpawning(false);

        // Find nearest waypoint to resume
        if (_patrolPath != null)
            _pathIndex = FindNearestWaypointIndex(transform.position);
    }

    public void OnDamageTaken()
    {
        if (_state != State.Dead)
        {
            // If attacked, we are likely close or hit by ranged. 
            // We ensure we are in Aggro mode so TickAggroBehavior runs and decides to flee if close.
            if (_state == State.Patrol || _state == State.ReturnToPatrol)
                EnterAggro();
        }
    }

    public void OnDeath()
    {
        _state = State.Dead;
        _motor.Stop();
        if (_animator) _animator.SetTrigger("Death");
        if (_spawner) _spawner.EnableSpawning(false);
        if (_spawner) _spawner.KillAllMinionsAnimated();

        enabled = false;
    }

    bool TrySense(out Transform target)
    {
        target = null;
        return _vision != null && _vision.TryGetClosestTarget(out target);
    }

    EnemyPatrolPath FindNearestPatrolPath()
    {
        var all = FindObjectsOfType<EnemyPatrolPath>();
        EnemyPatrolPath best = null;
        float minDist = float.MaxValue;

        foreach (var p in all)
        {
            float d = Vector2.Distance(transform.position, p.transform.position);
            if (d < minDist && d < _config.patrolPathSearchRadius)
            {
                minDist = d;
                best = p;
            }
        }

        return best;
    }

    int FindNearestWaypointIndex(Vector2 pos)
    {
        if (_patrolPath == null) return 0;
        int best = 0;
        float minDist = float.MaxValue;
        for (int i = 0; i < _patrolPath.Count; i++)
        {
            float d = Vector2.Distance(pos, _patrolPath.GetPoint(i));
            if (d < minDist)
            {
                minDist = d;
                best = i;
            }
        }

        return best;
    }

    void AdvancePathIndex()
    {
        if (_patrolPath == null) return;
        _pathIndex += _pathDir;
        if (_pathIndex >= _patrolPath.Count || _pathIndex < 0)
        {
            _pathDir *= -1;
            _pathIndex = Mathf.Clamp(_pathIndex, 0, _patrolPath.Count - 1);
        }
    }

    void UpdateAnimator()
    {
        // Add any parameter updates if needed (e.g. Speed)
    }

    void OnDrawGizmosSelected()
    {
        if (_config != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _config.visionRadius);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _config.fleeDistance);
        }

        if (_state == State.Evasion)
        {
            // Visualize flee destination
            if (_motor != null && _motor.Velocity.sqrMagnitude > 0.1f)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(transform.position, transform.position + (Vector3)_motor.Velocity);
            }
        }
    }
}
