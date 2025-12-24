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
    
    // Patrol
    float _orbitAngle;

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
    }

    public void Initialize(SwarmMinionSpawner owner)
    {
        _owner = owner;
    }

    public void OnSpawn()
    {
        _orbitAngle = Random.Range(0f, 360f);
        _supportLateralSide = Random.Range(0, 2) == 0 ? 1f : -1f;
        _currentRole = Role.Patrol;
        
        // Revive Logic if needed? Health component usually needs reset if pooled manually without Zenject MemoryPool
        // Assuming Health handles itself on Enable or we might need to reset it.
        // If Health is Monobehaviour and not destroyed, we should check if it needs reset.
        // For now assume standard usage.
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
        if (_vision && _vision.TryGetClosestTarget(out var seenTarget))
        {
            _owner.ReportEnemySeen(seenTarget);
        }

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

        // Animator logic (simple)
        if (_animator)
        {
            // _animator.SetBool("Aggro", _currentRole != Role.Patrol); // Removed as parameter does not exist
        }
    }

    void TickPatrol()
    {
        if (_config == null) return;

        // Orbit around owner
        _orbitAngle += _config.orbitSpeed * Time.deltaTime * Mathf.Rad2Deg; // config speed in rad/s, we use degrees for trig if needed or just mult
        // actually Mathf.Cos takes radians.
        
        float rads = _orbitAngle * Mathf.Deg2Rad;
        Vector2 offset = new Vector2(Mathf.Cos(rads), Mathf.Sin(rads)) * _config.orbitRadius;
        Vector2 target = (Vector2)_owner.transform.position + offset;

        _motor.MoveTo(target, _config.patrolSpeed);
    }

    void TickAggressor()
    {
        Transform target = _owner.SquadTarget;
        if (target == null) return;

        float dist = Vector2.Distance(transform.position, target.position);
        
        // Movement
        if (dist > _config.attackDistance)
        {
            _motor.MoveTo(target.position, _config.aggroSpeed);
        }
        else if (dist < _config.attackBackoffDistance)
        {
            // Back off
            Vector2 dir = (transform.position - target.position).normalized;
            Vector2 dest = (Vector2)target.position + dir * _config.attackDistance;
            _motor.MoveTo(dest, _config.aggroSpeed);
        }
        else
        {
            // In range
            _motor.Stop();
        }
        
        _motor.FaceTowards(target.position);

        // Shoot
        if (dist < _config.shootRange)
        {
            _shooter.TryFireAt(target.position);
        }
    }

    void TickSupport()
    {
        Transform target = _owner.SquadTarget;
        if (target == null) return;

        Vector2 toTarget = (Vector2)target.position - (Vector2)transform.position;
        Vector2 dirToTarget = toTarget.normalized;
        
        // Perpendicular vector for lateral offset
        Vector2 perp = new Vector2(-dirToTarget.y, dirToTarget.x) * _supportLateralSide;

        // Desired position: Target position - direction * supportDistance + perp * spread
        // Actually we want to stay AT supportDistance FROM target.
        // So target.position - dirToTarget * supportDistance...
        // But dirToTarget changes as we move.
        // Let's use the vector from Target TO Us to determine the "Sector".
        
        // Simply: Stay at supportDistance, offset by lateral spread relative to line of sight
        // Target - (DirectionFromTarget * Distance) + Lateral
        
        // Direction from Target to Minion
        Vector2 dirFromTarget = ((Vector2)transform.position - (Vector2)target.position).normalized;
        if (dirFromTarget == Vector2.zero) dirFromTarget = Vector2.right;
        
        Vector2 desiredPos = (Vector2)target.position + dirFromTarget * _config.supportDistance;
        
        // Add some lateral movement (orbiting slowly?)
        // Let's just hold position relative to player
        
        _motor.MoveTo(desiredPos, _config.patrolSpeed); // Use patrol speed for support movement
        _motor.FaceTowards(target.position);

        // Shoot occasionally
        if (Time.time > _nextSupportFireTime)
        {
             if (toTarget.magnitude < _config.detectRange) // Check range
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
