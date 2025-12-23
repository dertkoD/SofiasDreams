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

    [Header("Refs")]
    [SerializeField] SwarmEnemyMotor2D _motor;
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

        switch (_state)
        {
            case State.Patrol:
                TickPatrol();
                break;
            case State.Aggro:
                TickAggro(seesPlayer);
                break;
            case State.Evasion:
                TickEvasion();
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

    void TickAggro(bool seesPlayer)
    {
        if (!seesPlayer)
        {
            _forgetTimer -= Time.deltaTime;
            if (_forgetTimer <= 0)
            {
                EnterReturnToPatrol();
                return;
            }
        }

        if (_player == null) return;

        // Ensure we stop and spawn minions
        _motor.Stop();
        
        // Check distance for Evasion
        float dist = Vector2.Distance(transform.position, _player.position);
        if (dist < _config.fleeDistance)
        {
            EnterEvasion();
            return;
        }

        // Spawner logic is handled by Spawner itself monitoring Aggro state or we call it
        if (_spawner) _spawner.SetAggroTarget(_player);
    }

    void TickEvasion()
    {
        if (_player == null)
        {
            EnterReturnToPatrol();
            return;
        }

        // Flee away from player
        Vector2 dir = (transform.position - _player.position).normalized;
        Vector2 fleePos = (Vector2)transform.position + dir * 5.0f; // Look ahead 5 units
        
        _motor.MoveTo(fleePos, _config.fleeSpeed);

        if (Vector2.Distance(transform.position, _player.position) > _config.fleeDistance * 1.5f)
        {
            EnterAggro();
        }
    }

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

    void EnterEvasion()
    {
        _state = State.Evasion;
        // Keep Angry animation
        
        // Ensure move starts immediately
        TickEvasion();
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
            // If attacked, flee!
            EnterEvasion();
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
        
        foreach(var p in all)
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
        for(int i=0; i<_patrolPath.Count; i++)
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
    
    public void SetPatrolPath(EnemyPatrolPath path)
    {
        _patrolPath = path;
    }
}
