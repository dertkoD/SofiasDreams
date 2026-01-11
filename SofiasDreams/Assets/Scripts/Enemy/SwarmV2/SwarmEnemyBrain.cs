using UnityEngine;
using Zenject;
using System.Collections.Generic;

public class SwarmEnemyBrain : BaseEnemyBrain
{
    [Header("Refs")] 
    [SerializeField] SwarmEnemyMotor2D _motor;
    [SerializeField] SwarmMinionSpawner _spawner;
    [SerializeField] VisionCone2D _vision;
    [SerializeField] EnemyPatrolPath _patrolPath;
    [SerializeField] Animator _animator;
    [SerializeField] Health _health;

    public SwarmEnemyMotor2D Motor => _motor;
    public SwarmMinionSpawner Spawner => _spawner;
    public VisionCone2D Vision => _vision;
    public EnemyPatrolPath PatrolPath { get => _patrolPath; set => _patrolPath = value; }
    public Animator Animator => _animator;
    public Health Health => _health;

    public SwarmConfig Config { get; private set; }
    
    // States
    public SwarmPatrolState PatrolState { get; private set; }
    public SwarmAggroState AggroState { get; private set; }
    public SwarmEvasionState EvasionState { get; private set; }
    public SwarmReturnState ReturnState { get; private set; }
    public SwarmDeadState DeadState { get; private set; }

    // Runtime Data
    public Transform Player { get; set; }
    public Vector2 LastSeenPos { get; set; }
    public bool HasSeenPlayer { get; set; }
    public float ForgetTimer { get; set; }
    
    // Patrol Runtime
    public int PathIndex;
    public int PathDir = 1;

    [Inject]
    public void Construct(SwarmConfig config)
    {
        Config = config;
    }
    
    void Awake()
    {
        if (!_motor) _motor = GetComponent<SwarmEnemyMotor2D>();
        if (!_spawner) _spawner = GetComponent<SwarmMinionSpawner>();
        if (!_vision) _vision = GetComponentInChildren<VisionCone2D>();
        if (!_animator) _animator = GetComponentInChildren<Animator>();
        if (!_health) _health = GetComponent<Health>();
        if (!_patrolPath) _patrolPath = GetComponentInChildren<EnemyPatrolPath>(); // Try local first

        PatrolState = new SwarmPatrolState(this);
        AggroState = new SwarmAggroState(this);
        EvasionState = new SwarmEvasionState(this);
        ReturnState = new SwarmReturnState(this);
        DeadState = new SwarmDeadState(this);
    }

    void Start()
    {
        if (PatrolPath == null)
            PatrolPath = FindNearestPatrolPath();

        if (Player == null)
        {
            var pf = FindObjectOfType<PlayerFacade>();
            if (pf != null) Player = pf.transform;
        }

        if (PatrolPath != null && PatrolPath.Count > 0)
        {
            PathIndex = FindNearestWaypointIndex(transform.position);
        }
        
        ChangeState(PatrolState);
    }

    void OnEnable()
    {
        if (Health) Health.OnHealthChanged += OnHealthChanged;
    }

    void OnDisable()
    {
        if (Health) Health.OnHealthChanged -= OnHealthChanged;
    }

    void OnHealthChanged()
    {
        if (Health == null) return;
        if (!Health.IsAlive)
        {
            ChangeState(DeadState);
            return;
        }

        if (Health.LastHit != null && Health.LastHit.source != null)
        {
            if (Player == null || Player != Health.LastHit.source)
            {
                Player = Health.LastHit.source;
            }
            
            ForgetTimer = Config.aggroForgetSeconds;
            // Trigger Evasion immediately or Aggro? Original says "Trigger Evasion immediately" comment, 
            // but logic was just setting forget timer and OnDamageTaken calls EnterAggro.
            
            OnDamageTaken();
        }
    }
    
    public void OnDamageTaken()
    {
        if (CurrentState != DeadState)
        {
            if (CurrentState == PatrolState || CurrentState == ReturnState)
                ChangeState(AggroState);
        }
    }

    protected override void Update()
    {
        if (CurrentState == DeadState) return;

        bool seesPlayer = TrySense(out var target);
        if (seesPlayer)
        {
            Player = target;
            LastSeenPos = target.position;
            HasSeenPlayer = true;
            ForgetTimer = Config.aggroForgetSeconds;

            if (CurrentState != AggroState && CurrentState != EvasionState)
            {
                ChangeState(AggroState);
            }
        }
        else if (CurrentState == AggroState || CurrentState == EvasionState)
        {
            ForgetTimer -= Time.deltaTime;
            if (ForgetTimer <= 0f)
            {
                ChangeState(ReturnState);
            }
        }

        base.Update();
    }

    public bool TrySense(out Transform target)
    {
        target = null;
        return Vision != null && Vision.TryGetClosestTarget(out target);
    }

    EnemyPatrolPath FindNearestPatrolPath()
    {
        var all = FindObjectsOfType<EnemyPatrolPath>();
        EnemyPatrolPath best = null;
        float minDist = float.MaxValue;

        foreach (var p in all)
        {
            float d = Vector2.Distance(transform.position, p.transform.position);
            if (d < minDist && d < Config.patrolPathSearchRadius)
            {
                minDist = d;
                best = p;
            }
        }
        return best;
    }

    public int FindNearestWaypointIndex(Vector2 pos)
    {
        if (PatrolPath == null) return 0;
        int best = 0;
        float minDist = float.MaxValue;
        for (int i = 0; i < PatrolPath.Count; i++)
        {
            float d = Vector2.Distance(pos, PatrolPath.GetPoint(i));
            if (d < minDist) { minDist = d; best = i; }
        }
        return best;
    }

    public void AdvancePathIndex()
    {
        if (PatrolPath == null) return;
        PathIndex += PathDir;
        if (PathIndex >= PatrolPath.Count || PathIndex < 0)
        {
            PathDir *= -1;
            PathIndex = Mathf.Clamp(PathIndex, 0, PatrolPath.Count - 1);
        }
    }
    
    public void SetPatrolPath(EnemyPatrolPath path)
    {
        PatrolPath = path;
        if (PatrolPath != null && PatrolPath.Count > 0)
        {
            PathIndex = FindNearestWaypointIndex(transform.position);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (Config != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, Config.visionRadius);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, Config.fleeDistance);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, Config.minionOrbitRadius);
        }
        
        if (CurrentState == EvasionState && Motor != null && Motor.Velocity.sqrMagnitude > 0.1f)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)Motor.Velocity);
        }
    }
}

// --- States ---

public class SwarmPatrolState : IEnemyState
{
    SwarmEnemyBrain _brain;
    public SwarmPatrolState(SwarmEnemyBrain brain) { _brain = brain; }

    public void Enter() { }

    public void Tick()
    {
        if (_brain.PatrolPath == null || _brain.PatrolPath.Count == 0) return;

        Vector2 target = _brain.PatrolPath.GetPoint(_brain.PathIndex);
        _brain.Motor.MoveTo(target, _brain.Config.patrolSpeed);

        if (Vector2.Distance(_brain.transform.position, target) <= _brain.Config.waypointArriveDistance)
        {
            _brain.AdvancePathIndex();
        }
    }

    public void Exit() { }
}

public class SwarmAggroState : IEnemyState
{
    SwarmEnemyBrain _brain;
    public SwarmAggroState(SwarmEnemyBrain brain) { _brain = brain; }

    public void Enter()
    {
        if (_brain.Animator) _brain.Animator.SetTrigger("Angry");
        if (_brain.Spawner) _brain.Spawner.EnableSpawning(true);
    }

    public void Tick()
    {
        if (_brain.Player == null) return;

        float dist = Vector2.Distance(_brain.transform.position, _brain.Player.position);
        
        if (dist < _brain.Config.maintainDistance)
        {
            _brain.ChangeState(_brain.EvasionState);
            return;
        }

        // Stop and chill
        _brain.Motor.Stop();
        
        // Spawning Logic
        if (_brain.Spawner) _brain.Spawner.SetAggroTarget(_brain.Player);
    }

    public void Exit() { }
}

public class SwarmEvasionState : IEnemyState
{
    SwarmEnemyBrain _brain;
    public SwarmEvasionState(SwarmEnemyBrain brain) { _brain = brain; }

    public void Enter()
    {
        if (_brain.Spawner) _brain.Spawner.EnableSpawning(true);
    }

    public void Tick()
    {
        if (_brain.Player == null) return;

        float dist = Vector2.Distance(_brain.transform.position, _brain.Player.position);

        if (dist >= _brain.Config.maintainDistance)
        {
            _brain.ChangeState(_brain.AggroState);
            return;
        }

        // Flee away
        Vector2 dir = (_brain.transform.position - _brain.Player.position).normalized;
        Vector2 fleePos = (Vector2)_brain.transform.position + dir * 5.0f;
        _brain.Motor.MoveTo(fleePos, _brain.Config.fleeSpeed);
        
        if (_brain.Spawner) _brain.Spawner.SetAggroTarget(_brain.Player);
    }

    public void Exit() { }
}

public class SwarmReturnState : IEnemyState
{
    SwarmEnemyBrain _brain;
    public SwarmReturnState(SwarmEnemyBrain brain) { _brain = brain; }

    public void Enter()
    {
        if (_brain.Animator) _brain.Animator.SetTrigger("Idle");
        if (_brain.Spawner) _brain.Spawner.EnableSpawning(false);

        if (_brain.PatrolPath != null)
            _brain.PathIndex = _brain.FindNearestWaypointIndex(_brain.transform.position);
    }

    public void Tick()
    {
        if (_brain.PatrolPath == null)
        {
            _brain.ChangeState(_brain.PatrolState);
            return;
        }

        Vector2 target = _brain.PatrolPath.GetPoint(_brain.PathIndex);
        _brain.Motor.MoveTo(target, _brain.Config.patrolSpeed);

        if (Vector2.Distance(_brain.transform.position, target) <= _brain.Config.waypointArriveDistance)
        {
            _brain.ChangeState(_brain.PatrolState);
        }
    }

    public void Exit() { }
}

public class SwarmDeadState : IEnemyState
{
    SwarmEnemyBrain _brain;
    public SwarmDeadState(SwarmEnemyBrain brain) { _brain = brain; }

    public void Enter()
    {
        _brain.Motor.Stop();
        if (_brain.Animator) _brain.Animator.SetTrigger("Death");
        if (_brain.Spawner) _brain.Spawner.EnableSpawning(false);
        if (_brain.Spawner) _brain.Spawner.KillAllMinionsAnimated();
        
        _brain.enabled = false;
    }

    public void Tick() { }
    public void Exit() { }
}
