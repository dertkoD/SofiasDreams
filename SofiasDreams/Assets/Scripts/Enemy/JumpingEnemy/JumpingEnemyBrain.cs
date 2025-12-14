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

    // Patrol X-bounds (hard limits in Patrol state)
    bool _hasPatrolBounds;
    float _patrolMinX;
    float _patrolMaxX;

    // Resume patrol after aggro
    bool _hasSavedPatrolResume;
    EnemyPatrolPath _savedPath;
    int _savedPathIndex;
    int _savedPathDir;

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
        CachePatrolBounds();

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
                ApplyPatrolXBounds(true);
                TickPatrol();
                break;

            case State.AggroTrigger:
                ApplyPatrolXBounds(false);
                if (sees) _forgetLeft = _config != null ? _config.aggroForgetSeconds : 0f;
                TickAggroTrigger();
                break;

            case State.Aggro:
                ApplyPatrolXBounds(false);
                TickAggro(sees);
                break;

            case State.ReturnToPatrol:
                if (sees) { RequestAggroTrigger(); break; }
                ApplyPatrolXBounds(false);
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

            // Patrol progression uses X-boundaries, handled in TickPatrol / TickReturnToPatrol.

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

        // Patrol points are treated as X-boundaries. Enemy doesn't need to "touch" the point,
        // but must never cross it in X.
        if (_path == null || _path.Count == 0) return;

        float x = transform.position.x;
        float arrive = Mathf.Max(0.01f, _config.waypointArriveDistance);
        float veryClose = Mathf.Max(arrive, 0.15f);

        // If we're already close to one (or many) boundaries, consume them without hopping,
        // but avoid an infinite loop if all points are clustered.
        float boundaryX = _path.GetPoint(_pathIndex).x;
        float dx = boundaryX - x;
        int guard = 0;
        while (Mathf.Abs(dx) <= veryClose && guard++ < _path.Count)
        {
            AdvancePathIndex();
            boundaryX = _path.GetPoint(_pathIndex).x;
            dx = boundaryX - x;
        }

        // Still close after consuming many points => do nothing this tick.
        if (Mathf.Abs(dx) <= veryClose)
            return;

        float h = _config.patrolJumpHeight;
        float flightTime = EstimateFlightTimeSeconds(h);

        int dir = dx >= 0f ? +1 : -1;
        float s = _config.patrolJumpHorizontalSpeed;
        if (flightTime > 0.0001f)
        {
            // Cap speed so even full-flight horizontal travel cannot cross boundary.
            float maxSafeSpeed = Mathf.Abs(dx) / flightTime;
            s = Mathf.Min(s, maxSafeSpeed);
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

        // Desired behaviour:
        // - if player is visible: keep timer refreshed (not counting down)
        // - if player is NOT visible: countdown starts immediately
        if (sees) _forgetLeft = _config.aggroForgetSeconds;
        else _forgetLeft = Mathf.Max(0f, _forgetLeft - Time.deltaTime);

        if (_forgetLeft <= 0f)
        {
            // Stop chasing immediately even mid-air.
            // But do not play PatrolTrigger mid-air (it can freeze), queue it until landing.
            if (_motor.IsGrounded && IsStableOnGround())
            {
                BeginReturnToPatrol(playTrigger: true);
            }
            else
            {
                _pendingPatrolTrigger = true;
                BeginReturnToPatrol(playTrigger: false);
            }
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

        if (_path == null || _path.Count == 0) return;

        // Return to the saved patrol target boundary (by X), then continue Patrol.
        float boundaryX = _path.GetPoint(_pathIndex).x;
        float x = transform.position.x;
        float dx = boundaryX - x;

        float arrive = Mathf.Max(0.01f, _config.waypointArriveDistance);
        if (_motor.IsGrounded && Mathf.Abs(dx) <= arrive)
        {
            _state = State.Patrol;
            _nextJumpAt = Time.time;
            return;
        }

        // While in air, keep aiming at boundary.
        if (!_motor.IsGrounded && !_motor.IsFrozen)
        {
            int dirAir = dx >= 0f ? +1 : -1;
            _motor.SetAirDesiredVX(dirAir * Mathf.Max(0f, _config.patrolJumpHorizontalSpeed));
            return;
        }

        if (Time.time < _landingStunUntil) return;
        if (Time.time < _nextJumpAt) return;

        int dir = dx >= 0f ? +1 : -1;
        float h = _config.patrolJumpHeight;
        float flightTime = EstimateFlightTimeSeconds(h);

        float s = _config.patrolJumpHorizontalSpeed;
        if (flightTime > 0.0001f)
        {
            float maxSafeSpeed = Mathf.Abs(dx) / flightTime;
            s = Mathf.Min(s, maxSafeSpeed);
        }

        if (StartJump(dir, h, s))
            _nextJumpAt = Time.time + _config.patrolJumpCooldown;
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

        SavePatrolResumeStateIfNeeded();

        _state = State.AggroTrigger;
        _forgetLeft = _config.aggroForgetSeconds;
        _lostSightTimerRunning = false;

        _motor?.StopAll();
        _jumpBool = false;
        _anim?.SetJump(false);
        _anim?.TriggerAgro();
    }

    void BeginReturnToPatrol(bool playTrigger = true)
    {
        if (_state == State.Dead) return;
        // If we want to play trigger clips, NEVER trigger mid-air; queue until landing.
        if (playTrigger && (_jumpBool || !IsStableOnGround()))
        {
            _pendingPatrolTrigger = true;
            return;
        }

        _state = State.ReturnToPatrol;
        _hasLastSeen = false;
        _hasChaseDir = false;
        _hasSeenPlayerAtLeastOnce = false;
        _lostSightTimerRunning = false;
        if (playTrigger) _jumpBool = false;

        // Restore patrol route we had before aggro (resume from same target index + direction).
        if (_hasSavedPatrolResume && _savedPath != null && _savedPath.Count > 0)
        {
            _path = _savedPath;
            CachePatrolBounds();
            _pathIndex = Mathf.Clamp(_savedPathIndex, 0, _path.Count - 1);
            _pathDir = _savedPathDir == 0 ? 1 : (_savedPathDir > 0 ? 1 : -1);
        }
        else
        {
            if (_path == null || _path.Count == 0)
            {
                if (_patrolPath == null)
                    _patrolPath = FindNearestPatrolPath();
                _path = _patrolPath;
                CachePatrolBounds();
            }

            if (_path != null && _path.Count > 0)
                _pathIndex = FindNearestWaypointIndex(transform.position);
        }

        if (playTrigger)
        {
            _anim?.SetJump(false);
            _anim?.TriggerPatrol();
            _nextJumpAt = Time.time + 0.05f;
        }
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

        ApplyPatrolXBounds(false);

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

    void SavePatrolResumeStateIfNeeded()
    {
        // Save only when leaving patrol-ish states. This is our "return to the same route" anchor.
        if (_state != State.Patrol && _state != State.ReturnToPatrol)
            return;
        if (_path == null || _path.Count == 0)
            return;

        _hasSavedPatrolResume = true;
        _savedPath = _path;
        _savedPathIndex = _pathIndex;
        _savedPathDir = _pathDir;
    }

    void CachePatrolBounds()
    {
        _hasPatrolBounds = false;
        if (_path == null || _path.Count == 0) return;

        float min = float.PositiveInfinity;
        float max = float.NegativeInfinity;
        for (int i = 0; i < _path.Count; i++)
        {
            float x = _path.GetPoint(i).x;
            if (x < min) min = x;
            if (x > max) max = x;
        }

        if (!float.IsInfinity(min) && !float.IsInfinity(max))
        {
            _hasPatrolBounds = true;
            _patrolMinX = min;
            _patrolMaxX = max;
        }
    }

    void ApplyPatrolXBounds(bool enabled)
    {
        if (_motor == null) return;
        if (!_hasPatrolBounds)
        {
            if (_path != null && _path.Count > 0)
                CachePatrolBounds();
        }

        if (!_hasPatrolBounds)
        {
            _motor.SetXBounds(false, 0f, 0f);
            return;
        }

        float eps = Mathf.Max(0.01f, (_config != null ? _config.waypointArriveDistance : 0.05f));
        _motor.SetXBounds(enabled, _patrolMinX, _patrolMaxX, epsilon: eps);
    }

    float EstimateFlightTimeSeconds(float jumpHeight)
    {
        if (_motor == null || _motor.Rigidbody == null) return 0f;
        float g = Mathf.Abs(Physics2D.gravity.y * Mathf.Max(0f, _motor.Rigidbody.gravityScale));
        float H = Mathf.Max(0f, jumpHeight);
        if (g <= 0.0001f || H <= 0.0001f) return 0f;
        float tUp = Mathf.Sqrt(2f * H / g);
        return 2f * tUp;
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

