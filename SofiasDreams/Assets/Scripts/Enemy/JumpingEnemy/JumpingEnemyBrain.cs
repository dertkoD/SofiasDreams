using UnityEngine;
using Zenject;

public class JumpingEnemyBrain : MonoBehaviour
{
    enum State
    {
        Patrol,
        AggroTrigger,
        Aggro,
        ReturnToPatrol,
        Dead
    }

    [Header("Refs")]
    [SerializeField] JumpingEnemyMotor2D _motor;
    [SerializeField] JumpingEnemyAnimatorAdapter _anim;
    [SerializeField] VisionCone2D _vision;
    [SerializeField] Health _health;
    [SerializeField] EnemyPatrolPath _patrolPath;
    
    [Header("Config (fallback if DI is not used)")]
    [SerializeField] JumpingEnemyConfigSO _configOverride;

    JumpingEnemyConfigSO _config;
    IHealth _iHealth;

    State _state;
    Vector2 _spawnPos;

    // Patrol path runtime
    EnemyPatrolPath _path;
    int _pathIndex;
    int _pathDir = 1;

    // Aggro runtime
    float _forgetLeft;
    Vector2 _lastSeenPos;
    bool _hasLastSeen;

    // Jump loop runtime
    bool _jumpBool;
    float _nextJumpAt;
    bool _prevGrounded;

    // Damage watch
    int _lastHp = int.MinValue;
    bool _armedHpWatch;

    [Inject]
    public void Construct(
        [InjectOptional] JumpingEnemyConfigSO config = null,
        [InjectOptional] IHealth health = null)
    {
        _config = config != null ? config : _configOverride;
        _iHealth = health;
    }

    void Reset()
    {
        _motor = GetComponent<JumpingEnemyMotor2D>();
        _health = GetComponent<Health>();
        _vision = GetComponentInChildren<VisionCone2D>(true);
        _anim = GetComponentInChildren<JumpingEnemyAnimatorAdapter>(true);
        _patrolPath = GetComponentInChildren<EnemyPatrolPath>(true);
    }

    void Awake()
    {
        if (!_motor) _motor = GetComponent<JumpingEnemyMotor2D>();
        if (!_health) _health = GetComponent<Health>();
        if (!_vision) _vision = GetComponentInChildren<VisionCone2D>(true);
        if (!_anim) _anim = GetComponentInChildren<JumpingEnemyAnimatorAdapter>(true);
        if (!_patrolPath) _patrolPath = GetComponentInChildren<EnemyPatrolPath>(true);
        if (_config == null) _config = _configOverride;
        if (_iHealth == null) _iHealth = _health as IHealth;

        _spawnPos = transform.position;

        _path = _patrolPath;
        if (_path != null && _path.Count > 0)
            _pathIndex = FindNearestWaypointIndex(_spawnPos);

        _state = State.Patrol;
        _prevGrounded = _motor != null && _motor.IsGrounded;
    }

    void OnEnable()
    {
        if (_health != null)
            _health.OnHealthChanged += OnHealthChanged;

        ArmHpWatch();
    }

    void OnDisable()
    {
        if (_health != null)
            _health.OnHealthChanged -= OnHealthChanged;
    }

    void Update()
    {
        if (_config == null || _iHealth == null)
        {
            // Can't operate without config/health; log once and stop.
            Debug.LogError($"[JumpingEnemyBrain] Missing config/health on {name}. Assign JumpingEnemyConfigSO on this component (fallback) or use JumpingEnemyInstaller.");
            enabled = false;
            return;
        }

        if (!_iHealth.IsAlive)
        {
            EnterDead();
            return;
        }

        TickAnimatorParams();

        // Sensing / aggro extension
        bool sees = TrySense(out var target);
        if (sees)
        {
            _lastSeenPos = target.position;
            _hasLastSeen = true;
        }

        switch (_state)
        {
            case State.Patrol:
                if (sees) { EnterAggroTrigger(); break; }
                TickPatrol();
                break;

            case State.AggroTrigger:
                if (sees) _forgetLeft = _config != null ? _config.aggroForgetSeconds : 0f;
                TickAggroTrigger();
                break;

            case State.Aggro:
                TickAggro(sees);
                break;

            case State.ReturnToPatrol:
                if (sees) { EnterAggroTrigger(); break; }
                TickReturnToPatrol();
                break;
        }
    }

    void TickAnimatorParams()
    {
        if (_anim == null || _motor == null) return;

        bool grounded = _motor.IsGrounded;
        float positiveY = Mathf.Abs(_motor.Velocity.y) + 0.01f;

        if (_state == State.Aggro || _state == State.AggroTrigger)
            _anim.SetAttackYVelocity(positiveY);
        else
            _anim.SetPatrolYVelocity(positiveY);

        // keep Jump bool consistent with ground transitions (avoid clearing in the same frame we start a jump)
        if (_jumpBool)
        {
            // landed
            if (!_prevGrounded && grounded)
            {
                _jumpBool = false;
                _anim.SetJump(false);
            }
        }
        else
        {
            // became airborne unexpectedly (knockback etc)
            if (_prevGrounded && !grounded && _state != State.AggroTrigger)
            {
                _jumpBool = true;
                _anim.SetJump(true);
            }
        }

        _prevGrounded = grounded;
    }

    void TickPatrol()
    {
        if (_config == null || _motor == null) return;
        if (!_motor.IsGrounded) return;
        if (Time.time < _nextJumpAt) return;

        int dir = GetPatrolDirectionSign();
        if (StartJump(dir, _config.patrolJumpHeight, _config.patrolJumpHorizontalSpeed))
            _nextJumpAt = Time.time + _config.patrolJumpCooldown;
    }

    void TickAggroTrigger()
    {
        if (_anim == null) { _state = State.Aggro; return; }

        // Wait until animator leaves AgroTrigger and reaches Attack-loop (Attack / Blend Tree Agro)
        if (_anim.IsInAttackLoop())
        {
            _state = State.Aggro;
            _nextJumpAt = Time.time; // allow immediate first jump if grounded
            if (_config != null) _forgetLeft = _config.aggroForgetSeconds;
        }
    }

    void TickAggro(bool sees)
    {
        if (_config == null || _motor == null) return;

        if (sees) _forgetLeft = _config.aggroForgetSeconds;
        else _forgetLeft = Mathf.Max(0f, _forgetLeft - Time.deltaTime);

        if (_forgetLeft <= 0f)
        {
            BeginReturnToPatrol();
            return;
        }

        if (!_motor.IsGrounded) return;
        if (Time.time < _nextJumpAt) return;

        int dir = GetAggroDirectionSign();
        if (StartJump(dir, _config.aggroJumpHeight, _config.aggroJumpHorizontalSpeed))
            _nextJumpAt = Time.time + _config.aggroJumpCooldown;
    }

    void TickReturnToPatrol()
    {
        if (_config == null || _motor == null) return;

        Vector2 dst = GetReturnDestination(out bool hasDst);
        if (!hasDst)
        {
            _state = State.Patrol;
            return;
        }

        if (_motor.IsGrounded)
        {
            float arrive = Mathf.Max(0.01f, _config.returnArriveDistance);
            if (Vector2.Distance(transform.position, dst) <= arrive)
            {
                // snapped back to patrol zone
                if (_path != null && _path.Count > 0)
                    _pathIndex = FindNearestWaypointIndex(transform.position);

                _state = State.Patrol;
                return;
            }
        }

        if (!_motor.IsGrounded) return;
        if (Time.time < _nextJumpAt) return;

        int dir = (dst.x >= transform.position.x) ? +1 : -1;
        if (StartJump(dir, _config.patrolJumpHeight, _config.patrolJumpHorizontalSpeed))
            _nextJumpAt = Time.time + _config.patrolJumpCooldown;
    }

    bool StartJump(int dirSign, float height, float speed)
    {
        if (_anim != null && !_jumpBool)
        {
            _jumpBool = true;
            _anim.SetJump(true);
        }

        bool ok = _motor.TryJump(dirSign, height, speed);
        if (!ok && _anim != null)
        {
            // rollback animator if jump didn't happen
            _jumpBool = false;
            _anim.SetJump(false);
        }
        return ok;
    }

    void EnterAggroTrigger()
    {
        if (_state == State.Dead) return;
        if (_config == null) return;

        // already aggro: only refresh timer
        if (_state == State.Aggro || _state == State.AggroTrigger)
        {
            _forgetLeft = _config.aggroForgetSeconds;
            return;
        }

        _state = State.AggroTrigger;
        _forgetLeft = _config.aggroForgetSeconds;

        _motor?.StopHorizontal();
        _jumpBool = false;
        _anim?.SetJump(false);
        _anim?.TriggerAgro();
    }

    void BeginReturnToPatrol()
    {
        if (_state == State.Dead) return;

        _state = State.ReturnToPatrol;
        _hasLastSeen = false;
        _jumpBool = false;

        _anim?.SetJump(false);
        _anim?.TriggerPatrol();
        _nextJumpAt = Time.time + 0.05f;
    }

    void EnterDead()
    {
        if (_state == State.Dead) return;
        var prev = _state;
        _state = State.Dead;

        _motor?.StopHorizontal();
        if (_anim != null)
        {
            _anim.SetJump(false);

            // If we died while aggro/attack logic was active - play DeathFromAttack
            bool fromAttack = prev == State.Aggro || prev == State.AggroTrigger || _anim.IsInAttackLoop();
            if (fromAttack) _anim.TriggerDeathFromAttack();
            else _anim.TriggerDeathFromPatrol();
        }

        enabled = false;
    }

    bool TrySense(out Transform target)
    {
        target = null;
        if (_vision == null) return false;
        return _vision.TryGetClosestTarget(out target);
    }

    int GetAggroDirectionSign()
    {
        Vector2 dst = _hasLastSeen ? _lastSeenPos : (Vector2)_spawnPos;
        float dx = dst.x - transform.position.x;
        if (Mathf.Abs(dx) < 0.01f) dx = transform.localScale.x;
        return dx >= 0f ? +1 : -1;
    }

    int GetPatrolDirectionSign()
    {
        if (_path == null || _path.Count == 0)
            return transform.localScale.x >= 0f ? +1 : -1;

        Vector3 target = _path.GetPoint(_pathIndex);
        float dx = target.x - transform.position.x;

        float arrive = Mathf.Max(0.01f, _config != null ? _config.waypointArriveDistance : 0.2f);
        if (Mathf.Abs(dx) <= arrive)
        {
            AdvancePathIndex();
            target = _path.GetPoint(_pathIndex);
            dx = target.x - transform.position.x;
        }

        if (Mathf.Abs(dx) < 0.01f)
            dx = transform.localScale.x;

        return dx >= 0f ? +1 : -1;
    }

    void AdvancePathIndex()
    {
        if (_path == null || _path.Count <= 1) return;
        if (_config != null && _config.loopPath)
        {
            _pathIndex = (_pathIndex + 1) % _path.Count;
            return;
        }

        int next = _pathIndex + _pathDir;
        if (next >= _path.Count || next < 0)
        {
            _pathDir *= -1;
            next = Mathf.Clamp(_pathIndex + _pathDir, 0, _path.Count - 1);
        }

        _pathIndex = next;
    }

    Vector2 GetReturnDestination(out bool hasDestination)
    {
        hasDestination = true;

        if (_config != null && _config.returnToNearestWaypoint && _path != null && _path.Count > 0)
        {
            int idx = FindNearestWaypointIndex(transform.position);
            return _path.GetPoint(idx);
        }

        return _spawnPos;
    }

    int FindNearestWaypointIndex(Vector2 pos)
    {
        if (_path == null || _path.Count == 0) return 0;

        int bestIndex = 0;
        float best = float.PositiveInfinity;
        for (int i = 0; i < _path.Count; i++)
        {
            Vector2 p = _path.GetPoint(i);
            float d = (p - pos).sqrMagnitude;
            if (d < best) { best = d; bestIndex = i; }
        }

        return bestIndex;
    }

    void OnHealthChanged()
    {
        if (_health == null || _iHealth == null) return;
        if (!_iHealth.IsAlive) return;

        // Trigger aggro when HP decreased (player attacked enemy)
        if (_armedHpWatch)
        {
            int hp = _health.CurrentHP;
            if (_lastHp != int.MinValue && hp < _lastHp)
                EnterAggroTrigger();
            _lastHp = hp;
        }
    }

    void ArmHpWatch()
    {
        if (_health == null) return;
        _lastHp = _health.CurrentHP;
        _armedHpWatch = true;
    }
}

