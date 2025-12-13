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

    JumpingEnemyConfigSO _config;
    IHealth _iHealth;
    SignalBus _bus;
    Transform _player;
    bool _hasSeenPlayerAtLeastOnce;

    State _state;
    Vector2 _spawnPos;

    // Patrol path runtime
    EnemyPatrolPath _path;
    int _pathIndex;
    int _pathDir = 1;
    bool _patrolJumpHasTarget;
    Vector2 _patrolJumpTarget;
    int _patrolDxSignAtJump;
    bool _returningToRoute;
    int _returnTargetIndex;
    bool _returnJumpHasTarget;
    Vector2 _returnJumpTarget;
    int _returnDxSignAtJump;

    // Aggro runtime
    float _forgetLeft;
    bool _lostSightTimerRunning;
    Vector2 _lastSeenPos;
    bool _hasLastSeen;
    int _lastChaseDirSign = +1;
    bool _hasChaseDir;

    // Jump loop runtime
    bool _jumpBool;
    float _nextJumpAt;
    bool _prevGrounded;
    float _prevY;
    float _landingStunUntil;
    float _lastJumpStartedAt;
    bool _pendingAggroTrigger;
    bool _pendingPatrolTrigger;

    // Damage watch
    int _lastHp = int.MinValue;
    bool _armedHpWatch;

    [Inject]
    public void Construct(JumpingEnemyConfigSO config, IHealth health, SignalBus bus)
    {
        _config = config;
        _iHealth = health;
        _bus = bus;
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
        if (_iHealth == null) _iHealth = _health as IHealth;

        _spawnPos = transform.position;

        if (_patrolPath == null)
            _patrolPath = FindNearestPatrolPath();

        _path = _patrolPath;
        if (_path != null && _path.Count > 0)
            _pathIndex = FindNearestWaypointIndex(_spawnPos);

        _state = State.Patrol;
        _prevGrounded = _motor != null && _motor.IsGrounded;
        _prevY = _motor != null ? _motor.Velocity.y : 0f;
    }

    void OnEnable()
    {
        if (_health != null)
            _health.OnHealthChanged += OnHealthChanged;

        if (_bus != null)
            _bus.Subscribe<PlayerSpawned>(OnPlayerSpawned);

        ArmHpWatch();
    }

    void OnDisable()
    {
        if (_health != null)
            _health.OnHealthChanged -= OnHealthChanged;

        if (_bus != null)
            _bus.TryUnsubscribe<PlayerSpawned>(OnPlayerSpawned);
    }

    void Start()
    {
        // Bootstrapper spawns player before enemies, but if this enemy enabled earlier, it can miss the signal.
        // Fallback: try to grab existing player once.
        if (_player == null)
        {
            var pf = FindObjectOfType<PlayerFacade>();
            if (pf != null) _player = pf.transform;
        }
    }

    void Update()
    {
        if (_config == null || _iHealth == null) return;

        if (!_iHealth.IsAlive)
        {
            EnterDead();
            return;
        }

        // While trigger-clips play, enemy must not move at all.
        // But NEVER freeze in mid-air (otherwise it hangs). Only freeze when stably grounded.
        if (_anim != null && _motor != null)
        {
            bool inTrigger = _anim.IsInAgroTrigger() || _anim.IsInPatrolTrigger();
            bool stableGround = IsStableOnGround();
            // Hard safety: never freeze while our jump cycle is active (prevents hanging in air).
            _motor.SetFrozen(inTrigger && stableGround && !_jumpBool);
        }

        TickAnimatorParams();

        // Sensing / aggro extension
        bool sees = TrySense(out var target);
        if (sees)
        {
            _lastSeenPos = target.position;
            _hasLastSeen = true;
            float dx = _lastSeenPos.x - transform.position.x;
            if (Mathf.Abs(dx) > 0.01f)
            {
                _lastChaseDirSign = dx >= 0f ? +1 : -1;
                _hasChaseDir = true;
            }

            _hasSeenPlayerAtLeastOnce = true;
            if (_player == null)
                _player = target;
        }

        switch (_state)
        {
            case State.Patrol:
                if (sees) { RequestAggroTrigger(); break; }
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
                if (sees) { RequestAggroTrigger(); break; }
                TickReturnToPatrol();
                break;
        }

        // Continuous pursuit in air during aggro (player can move after takeoff)
        if (_state == State.Aggro && _motor != null && !_motor.IsGrounded && !_motor.IsFrozen)
        {
            int dir = GetAggroDirectionSign();
            _motor.SetAirDesiredVX(dir * Mathf.Max(0f, _config.aggroJumpHorizontalSpeed));
        }
    }

    void TickAnimatorParams()
    {
        if (_anim == null || _motor == null) return;

        bool grounded = _motor.IsGrounded;
        float y = _motor.Velocity.y;

        // We only drive Jump during our own jump cycles:
        // jumping => Jump=true and yVelocity*>0
        // landed  => Jump=false and yVelocity*=0
        bool landedByGround = _jumpBool && !_prevGrounded && grounded;

        // Fallback landing detection (helps if groundMask is misconfigured):
        // detect "settled after falling": was going down, now almost stopped.
        bool landedByVelocity = _jumpBool
            && (Time.time - _lastJumpStartedAt) > 0.05f
            && _prevY < -0.10f
            && Mathf.Abs(y) < 0.02f;

        if (landedByGround || landedByVelocity)
        {
            _jumpBool = false;
            _anim.SetJump(false);

            // Patrol waypoint progression should happen on landing (prevents "circling" around a point).
            if (_state == State.Patrol)
                TryAdvancePatrolWaypointOnLanding();
            else if (_state == State.ReturnToPatrol)
                TryCompleteReturnToRouteOnLanding();

            // Queue triggers if they were requested mid-air (never enter trigger states in-flight)
            if (_pendingAggroTrigger)
            {
                _pendingAggroTrigger = false;
                _pendingPatrolTrigger = false; // aggro has priority
                EnterAggroTrigger();
            }
            else if (_pendingPatrolTrigger)
            {
                _pendingPatrolTrigger = false;
                BeginReturnToPatrol();
            }

            float stun = _config != null ? Mathf.Max(0f, _config.landingStunSeconds) : 0.10f;
            _landingStunUntil = Mathf.Max(_landingStunUntil, Time.time + stun);
            _nextJumpAt = Mathf.Max(_nextJumpAt, _landingStunUntil);
        }

        float yParam = _jumpBool ? (Mathf.Abs(y) + 0.01f) : 0f;

        if (_state == State.Aggro || _state == State.AggroTrigger)
            _anim.SetAttackYVelocity(yParam);
        else
            _anim.SetPatrolYVelocity(yParam);

        _prevGrounded = grounded;
        _prevY = y;
    }

    void TickPatrol()
    {
        if (_config == null || _motor == null) return;
        if (!_motor.IsGrounded) return;
        if (Time.time < _landingStunUntil) return;
        if (Time.time < _nextJumpAt) return;

        // If we are already at current waypoint (common after returning from aggro),
        // advance index now so we continue along the route instead of hopping around the same point.
        AdvancePatrolIndexIfAtWaypoint();

        int dir = GetPatrolDirectionSign(out var patrolTarget, out bool hasTarget);
        float h = _config.patrolJumpHeight;
        float s = _config.patrolJumpHorizontalSpeed;

        if (hasTarget)
        {
            _patrolJumpHasTarget = true;
            _patrolJumpTarget = patrolTarget;
            float dx = patrolTarget.x - transform.position.x;
            _patrolDxSignAtJump = Mathf.Abs(dx) < 0.001f ? (transform.localScale.x >= 0f ? +1 : -1) : (dx >= 0f ? +1 : -1);
        }
        else
        {
            _patrolJumpHasTarget = false;
        }

        if (StartJump(dir, h, s))
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
            _lostSightTimerRunning = false;
        }
    }

    void TickAggro(bool sees)
    {
        if (_config == null || _motor == null) return;

        // Forget timer behaviour:
        // - while player is visible: refresh timer and stop countdown
        // - when player becomes NOT visible: start countdown
        // - if player is seen again: refresh + stop countdown
        if (sees)
        {
            _forgetLeft = _config.aggroForgetSeconds;
            _lostSightTimerRunning = false;
        }
        else
        {
            if (!_lostSightTimerRunning)
            {
                _lostSightTimerRunning = true; // start countdown from next frame
            }
            else
            {
                _forgetLeft = Mathf.Max(0f, _forgetLeft - Time.deltaTime);
            }
        }

        if (_forgetLeft <= 0f)
        {
            // Do not start PatrolTrigger mid-air (would freeze in air). Queue it until landing.
            if (_motor.IsGrounded) BeginReturnToPatrol();
            else _pendingPatrolTrigger = true;
            return;
        }

        if (!_motor.IsGrounded) return;
        if (Time.time < _landingStunUntil) return;
        if (Time.time < _nextJumpAt) return;

        int dir = GetAggroDirectionSign();
        float h = _config.aggroJumpHeight;
        float s = _config.aggroJumpHorizontalSpeed;

        if (StartJump(dir, h, s))
            _nextJumpAt = Time.time + _config.aggroJumpCooldown;
    }

    void TickReturnToPatrol()
    {
        if (_config == null || _motor == null) return;

        // Animator can be in PatrolTrigger after Attack->PatrolTrigger transition: stay locked until it ends.
        if (_anim != null && _anim.IsInPatrolTrigger())
            return;

        // Return back onto patrol route: go to a chosen rejoin waypoint, then continue route normally.
        if (_path != null && _path.Count > 0 && _returningToRoute)
        {
            Vector2 dst = _path.GetPoint(_returnTargetIndex);

            // While in air, keep aiming at waypoint (helps after wall cancels X at takeoff)
            if (!_motor.IsGrounded && !_motor.IsFrozen)
            {
                int dirAir = dst.x >= transform.position.x ? +1 : -1;
                _motor.SetAirDesiredVX(dirAir * Mathf.Max(0f, _config.patrolJumpHorizontalSpeed));
            }

            // If we are already at the rejoin point, finish return immediately.
            float arrive = Mathf.Max(0.01f, _config.waypointArriveDistance);
            if (_motor.IsGrounded && Vector2.Distance(transform.position, dst) <= arrive)
            {
                _returningToRoute = false;
                // Next patrol jump should go to the NEXT waypoint.
                AdvancePathIndex();
                _state = State.Patrol;
                return;
            }

            if (!_motor.IsGrounded) return;
            if (Time.time < _landingStunUntil) return;
            if (Time.time < _nextJumpAt) return;

            int dir = (dst.x >= transform.position.x) ? +1 : -1;
            float h = _config.patrolJumpHeight;
            float s = _config.patrolJumpHorizontalSpeed;

            // Track return target for "passed waypoint" detection on landing.
            _returnJumpHasTarget = true;
            _returnJumpTarget = dst;
            float dx = dst.x - transform.position.x;
            _returnDxSignAtJump = Mathf.Abs(dx) < 0.001f
                ? (transform.localScale.x >= 0f ? +1 : -1)
                : (dx >= 0f ? +1 : -1);

            if (StartJump(dir, h, s))
                _nextJumpAt = Time.time + _config.patrolJumpCooldown;

            return;
        }
    }

    bool StartJump(int dirSign, float height, float speed)
    {
        if (_config == null || _motor == null || _anim == null) return false;
        if (Time.time < _landingStunUntil) return false;

        bool ok = _motor.TryJump(dirSign, height, speed);
        if (!ok) return false;

        _jumpBool = true;
        _lastJumpStartedAt = Time.time;
        _anim.SetJump(true);
        return true;
    }

    void EnterAggroTrigger()
    {
        if (_state == State.Dead) return;
        if (_config == null) return;
        if (!IsStableOnGround())
        {
            _pendingAggroTrigger = true;
            _forgetLeft = _config.aggroForgetSeconds;
            return;
        }

        // already aggro: only refresh timer
        if (_state == State.Aggro || _state == State.AggroTrigger)
        {
            _forgetLeft = _config.aggroForgetSeconds;
            return;
        }

        _state = State.AggroTrigger;
        _forgetLeft = _config.aggroForgetSeconds;
        _lostSightTimerRunning = false;

        _motor?.StopAll();
        _jumpBool = false;
        _anim?.SetJump(false);
        _anim?.TriggerAgro();
    }

    void BeginReturnToPatrol()
    {
        if (_state == State.Dead) return;
        // If we're in a jump cycle, NEVER trigger mid-air; queue until landing.
        if (_jumpBool || !IsStableOnGround())
        {
            _pendingPatrolTrigger = true;
            return;
        }

        _state = State.ReturnToPatrol;
        _hasLastSeen = false;
        _hasChaseDir = false;
        _hasSeenPlayerAtLeastOnce = false;
        _lostSightTimerRunning = false;
        _jumpBool = false;

        // Pick route rejoin once, then follow route normally.
        if (_path == null || _path.Count == 0)
        {
            if (_patrolPath == null)
                _patrolPath = FindNearestPatrolPath();
            _path = _patrolPath;
        }

        if (_path != null && _path.Count > 0)
        {
            _returningToRoute = true;
            _returnTargetIndex = FindNearestWaypointIndex(transform.position);
            _pathIndex = _returnTargetIndex;
        }
        else
        {
            _returningToRoute = false;
        }

        _anim?.SetJump(false);
        _anim?.TriggerPatrol();
        _nextJumpAt = Time.time + 0.05f;
    }

    void AdvancePatrolIndexIfAtWaypoint()
    {
        if (_path == null || _path.Count == 0 || _config == null) return;
        Vector2 cur = _path.GetPoint(_pathIndex);
        float arrive = Mathf.Max(0.01f, _config.waypointArriveDistance);
        if (Vector2.Distance(transform.position, cur) <= arrive)
            AdvancePathIndex();
    }

    void RequestAggroTrigger()
    {
        if (_state == State.Dead) return;
        // If we're in a jump cycle, NEVER trigger mid-air; queue until landing.
        if (_jumpBool || !IsStableOnGround())
        {
            _pendingAggroTrigger = true;
            _pendingPatrolTrigger = false;
            if (_config != null) _forgetLeft = _config.aggroForgetSeconds;
            return;
        }

        EnterAggroTrigger();
    }

    bool IsStableOnGround()
    {
        if (_motor == null || _config == null) return false;
        if (!_motor.IsGrounded) return false;
        if (_jumpBool) return false;
        return Mathf.Abs(_motor.Velocity.y) <= Mathf.Max(0f, _config.groundedVelocityEpsilon);
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
        // Requirement: once enemy has seen player at least once, it keeps chasing him
        // until timer ends or enemy dies (even if player is out of vision).
        if (_hasSeenPlayerAtLeastOnce && _player != null)
        {
            float dx = _player.position.x - transform.position.x;
            if (Mathf.Abs(dx) < 0.01f) dx = transform.localScale.x;
            int sign = dx >= 0f ? +1 : -1;
            _lastChaseDirSign = sign;
            _hasChaseDir = true;
            return sign;
        }

        // Before first visual contact: use last seen (if any) or keep moving in facing direction.
        if (_hasLastSeen)
        {
            float dx = _lastSeenPos.x - transform.position.x;
            if (Mathf.Abs(dx) < 0.01f) dx = transform.localScale.x;
            int sign = dx >= 0f ? +1 : -1;
            _lastChaseDirSign = sign;
            _hasChaseDir = true;
            return sign;
        }

        return _hasChaseDir ? _lastChaseDirSign : (transform.localScale.x >= 0f ? +1 : -1);
    }

    void OnPlayerSpawned(PlayerSpawned s)
    {
        if (s.facade != null)
            _player = s.facade.transform;
    }

    EnemyPatrolPath FindNearestPatrolPath()
    {
        var all = FindObjectsOfType<EnemyPatrolPath>(true);
        if (all == null || all.Length == 0) return null;

        float best = float.PositiveInfinity;
        EnemyPatrolPath bestPath = null;
        Vector2 pos = transform.position;
        float radius = _config != null ? Mathf.Max(0f, _config.patrolPathSearchRadius) : 100f;

        for (int i = 0; i < all.Length; i++)
        {
            var p = all[i];
            if (p == null || p.Count == 0) continue;
            float d = Vector2.Distance(pos, p.transform.position);
            if (d <= radius && d < best)
            {
                best = d;
                bestPath = p;
            }
        }

        return bestPath;
    }

    int GetPatrolDirectionSign(out Vector2 target, out bool hasTarget)
    {
        if (_path == null || _path.Count == 0)
        {
            target = _spawnPos;
            hasTarget = false;
            return transform.localScale.x >= 0f ? +1 : -1;
        }

        Vector3 t = _path.GetPoint(_pathIndex);
        float dx = t.x - transform.position.x;

        if (Mathf.Abs(dx) < 0.01f)
            dx = transform.localScale.x;

        target = t;
        hasTarget = true;
        return dx >= 0f ? +1 : -1;
    }

    void TryAdvancePatrolWaypointOnLanding()
    {
        if (!_patrolJumpHasTarget || _path == null || _path.Count == 0 || _config == null)
            return;

        float arrive = Mathf.Max(0.01f, _config.waypointArriveDistance);
        float dist = Vector2.Distance((Vector2)transform.position, _patrolJumpTarget);

        float dxNow = _patrolJumpTarget.x - transform.position.x;
        int dxSignNow = Mathf.Abs(dxNow) < 0.001f ? _patrolDxSignAtJump : (dxNow >= 0f ? +1 : -1);

        // Arrived (close enough) OR passed the waypoint (dx sign flipped since jump started)
        if (dist <= arrive || dxSignNow != _patrolDxSignAtJump)
            AdvancePathIndex();

        _patrolJumpHasTarget = false;
    }

    void TryCompleteReturnToRouteOnLanding()
    {
        if (!_returningToRoute || !_returnJumpHasTarget || _path == null || _path.Count == 0 || _config == null)
            return;

        float arrive = Mathf.Max(0.01f, _config.waypointArriveDistance);
        float dist = Vector2.Distance((Vector2)transform.position, _returnJumpTarget);

        float dxNow = _returnJumpTarget.x - transform.position.x;
        int dxSignNow = Mathf.Abs(dxNow) < 0.001f ? _returnDxSignAtJump : (dxNow >= 0f ? +1 : -1);

        // Arrived (close enough) OR passed the waypoint (dx sign flipped since jump started)
        bool reached = dist <= arrive || dxSignNow != _returnDxSignAtJump;
        _returnJumpHasTarget = false;

        if (!reached)
            return;

        // We are back on route: continue to next waypoint.
        _returningToRoute = false;
        _pathIndex = _returnTargetIndex;
        AdvancePathIndex();
        _state = State.Patrol;
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
                RequestAggroTrigger();
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

