using UnityEngine;
using Zenject;

public class MinionBrain : MonoBehaviour
{
    public enum Role { Patrol, Aggressor, Support }

    [Header("Refs")]
    [SerializeField] MinionMotor2D _motor;
    [SerializeField] MinionShooter2D _shooter;
    [SerializeField] VisionCone2D _vision;
    [SerializeField] Animator _animator;
    [SerializeField] Health _health;

    MinionConfig _config;
    SwarmMinionSpawner _owner;
    Role _currentRole;
    Collider2D _collider;
    
    // Aggro/Forget
    // float _localForgetTimer; // Removed: reliance on squad state
    bool _canSeeTarget; // Cached vision state

    // Patrol
    float _orbitAngle;
    float _orbitDirection = 1f; // 1 for clockwise, -1 for counter-clockwise
    float _spawnExitTimer; // Time to just fly away from center before orbiting
    float _stuckFlipCooldown; // Cooldown for flipping direction when stuck

    // Support
    float _supportLateralSide; // 1 or -1
    float _nextSupportFireTime;

    [Inject]
    public void Construct(MinionConfig config)
    {
        _config = config;
    }

    void Awake()
    {
        if (!_motor) _motor = GetComponent<MinionMotor2D>();
        if (!_shooter) _shooter = GetComponent<MinionShooter2D>();
        if (!_vision) _vision = GetComponentInChildren<VisionCone2D>();
        if (!_animator) _animator = GetComponentInChildren<Animator>();
        if (!_health) _health = GetComponent<Health>();
        
        _collider = GetComponent<Collider2D>();
        
        if (_vision) _vision.useTransformRotation = true;
    }

    public void Initialize(SwarmMinionSpawner owner)
    {
        _owner = owner;
        
        // Ignore collisions with owner
        if (_owner && _collider)
        {
            var ownerColliders = _owner.GetComponentsInChildren<Collider2D>();
            foreach (var c in ownerColliders)
            {
                if (c && c != _collider)
                    Physics2D.IgnoreCollision(_collider, c, true);
            }
        }
    }

    public void OnSpawn()
    {
        _orbitAngle = Random.Range(0f, 360f);
        _orbitDirection = Random.Range(0, 2) == 0 ? 1f : -1f;
        _supportLateralSide = Random.Range(0, 2) == 0 ? 1f : -1f;
        _currentRole = Role.Patrol;
        // _localForgetTimer = 0f; // Removed
        _spawnExitTimer = 0.5f; // Give 0.5s to exit spawn area

        // Ensure health is full when spawned from pool
        if (_health)
        {
            if (_health.CanHeal())
                _health.Heal(_health.MaxHP);
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

    void Update()
    {
        if (_owner == null) return;
        if (!_health.IsAlive) return;

        // Vision Check
        Transform seenTarget = null;
        _canSeeTarget = _vision && _vision.TryGetClosestTarget(out seenTarget);
        if (_canSeeTarget)
        {
            _owner.ReportEnemySeen(seenTarget);
        }

        // Behavior: Always follow role assigned by Squad Leader
        switch (_currentRole)
        {
            case Role.Patrol:
                TickPatrol();
                break;
            case Role.Aggressor:
                TickAggressor();
                break;
            case Role.Support:
                TickSupport();
                break;
        }
    }

    void TickPatrol()
    {
        if (_config == null) return;

        // Spawn Exit Logic
        if (_spawnExitTimer > 0f)
        {
            _spawnExitTimer -= Time.deltaTime;
            if (_owner != null)
            {
                Vector2 dir = ((Vector2)transform.position - (Vector2)_owner.transform.position).normalized;
                if (dir == Vector2.zero) dir = Random.insideUnitCircle.normalized;
                
                float initialOrbitRadius = _owner.MinionOrbitRadius;
                Vector2 targetPos = (Vector2)_owner.transform.position + dir * initialOrbitRadius;
                _motor.MoveTo(targetPos, _config.patrolSpeed);
            }
            return;
        }

        if (_owner == null) return;

        // Reactive Orbit Logic:
        // 1. Calculate current angle from owner
        Vector2 toMinion = (Vector2)transform.position - (Vector2)_owner.transform.position;
        float currentAngleRad = Mathf.Atan2(toMinion.y, toMinion.x);
        float orbitRadius = _owner.MinionOrbitRadius;

        // Check if stuck
        if (_stuckFlipCooldown > 0f)
        {
            _stuckFlipCooldown -= Time.deltaTime;
        }
        else if (_motor.Velocity.sqrMagnitude < 0.1f && _motor.IsMoving)
        {
             _orbitDirection *= -1f;
             _stuckFlipCooldown = 1.0f; // Wait 1s before flipping again
        }

        // 2. Project target ahead on the circle
        // Tangential speed = patrolSpeed. Angular speed = v / r
        float angularSpeed = _config.patrolSpeed / Mathf.Max(0.1f, orbitRadius);
        float lookAheadTime = 0.5f; // Look ahead 0.5s
        float nextAngleRad = currentAngleRad + (angularSpeed * _orbitDirection * lookAheadTime);

        // 3. Calculate target position
        Vector2 offset = new Vector2(Mathf.Cos(nextAngleRad), Mathf.Sin(nextAngleRad)) * orbitRadius;
        Vector2 target = (Vector2)_owner.transform.position + offset;

        // 4. Move there
        _motor.MoveTo(target, _config.patrolSpeed);
    }

    void TickAggressor()
    {
        Transform target = _owner.SquadTarget;
        if (target == null) return;

        float dist = Vector2.Distance(transform.position, target.position);
        
        // Strict distance maintenance
        if (dist > _config.attackDistance + 0.5f)
        {
            // Move Closer
            _motor.MoveTo(target.position, _config.aggroSpeed);
        }
        else if (dist < _config.attackDistance - 0.5f)
        {
            // Back off (move away from target)
            Vector2 dir = (transform.position - target.position).normalized;
            // Target specific point away
            Vector2 dest = (Vector2)target.position + dir * (_config.attackDistance + 1.0f);
            _motor.MoveTo(dest, _config.aggroSpeed);
        }
        else
        {
            // In optimal range
            _motor.Stop();
        }
        
        _motor.FaceTowards(target.position);

        if (dist < _config.shootRange)
        {
            _shooter.TryFireAt(target.position);
        }
    }

    void TickSupport()
    {
        Transform target = _owner.SquadTarget;
        if (target == null) return;

        // Halfway between Swarm and Aggressor
        Vector2 p1 = _owner.transform.position;
        Vector2 p2 = p1;
        
        if (_owner.CurrentAggressor != null)
        {
            p2 = _owner.CurrentAggressor.transform.position;
        }
        else
        {
            // Fallback if no aggressor (unlikely in Aggro state, but possible)
            // Use target position as proxy
            p2 = target.position;
        }

        Vector2 midPoint = (p1 + p2) * 0.5f;

        // Add some jitter/spread so they don't stack perfectly
        // We can use the _supportLateralSide and orbit logic here too, or just simple offset
        // Let's use the offset from center relative to target direction
        Vector2 dirToTarget = ((Vector2)target.position - midPoint).normalized;
        Vector2 perp = new Vector2(-dirToTarget.y, dirToTarget.x) * _supportLateralSide * _config.supportLateralSpread;

        Vector2 desiredPos = midPoint + perp;
        
        _motor.MoveTo(desiredPos, _config.patrolSpeed);
        _motor.FaceTowards(target.position);

        // Shoot occasionally if in range and line of sight
        float distToTarget = Vector2.Distance(transform.position, target.position);
        if (Time.time > _nextSupportFireTime)
        {
             if (_canSeeTarget && distToTarget < _config.shootRange) 
             {
                 _shooter.TryFireAt(target.position);
             }
             _nextSupportFireTime = Time.time + Random.Range(_config.supportFireIntervalMin, _config.supportFireIntervalMax);
        }
    }

    public void SetRole(Role role)
    {
        if (_currentRole != role)
        {
            _currentRole = role;
            if (role == Role.Support)
            {
                _nextSupportFireTime = Time.time + Random.Range(0.5f, 1.5f);
            }
            // Reset orbit angle when switching back to Patrol so we don't fly across map
            if (role == Role.Patrol && _owner != null)
            {
                // No state reset needed for new reactive logic
                _spawnExitTimer = 0f; 
            }
        }
    }

    void OnHealthChanged()
    {
        if (!_health.IsAlive)
        {
            Kill();
            return;
        }
        
        // If damaged, report to owner (if we know the source, ideally Health passes it)
        if (_health.LastHit != null && _health.LastHit.source != null)
        {
            _owner.ReportEnemySeen(_health.LastHit.source);
        }
    }

    public void Kill()
    {
        // Disable motor
        _motor.Stop();
        enabled = false;
        
        if (_animator)
        {
            _animator.SetTrigger("Death");
            // Safety fallback in case animation event is missing
            StartCoroutine(WaitAndRelease(2f));
        }
        else
        {
            Release();
        }
    }

    System.Collections.IEnumerator WaitAndRelease(float delay)
    {
        yield return new WaitForSeconds(delay);
        Release();
    }
    
    // Called by animation event "DeathComplete" or similar
    public void Release()
    {
        _owner.Release(this);
    }
}
