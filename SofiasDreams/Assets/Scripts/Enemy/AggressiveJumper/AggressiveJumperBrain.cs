using UnityEngine;
using Zenject;

[DisallowMultipleComponent]
[RequireComponent(typeof(AggressiveJumperJumpController))]
public class AggressiveJumperBrain : MonoBehaviour
{
    enum BehaviourState
    {
        Patrol,
        Agro,
        Dead
    }

    [Header("Config")]
    [SerializeField] AggressiveJumperConfigSO _config;

    [Header("Refs")]
    [SerializeField] VisionCone2D _vision;
    [SerializeField] EnemyPatrolPath _patrolPath;
    [SerializeField] AggressiveJumperJumpController _jumpController;
    [SerializeField] Animator _animator;
    [SerializeField] EnemyDamageReceiver _damageReceiver;
    [SerializeField] Health _health;
    [SerializeField] Transform _visualRoot;
    [SerializeField] EnemyFacade _facade;

    [Header("Animator params")]
    [SerializeField] string _agroTrigger = "Agro";
    [SerializeField] string _returnToPatrolTrigger = "Patrol";
    [SerializeField] string _deathFromPatrolBool = "DeathFromPatrol";
    [SerializeField] string _deathFromAttackTrigger = "DeathFromAttack";

    SignalBus _bus;

    BehaviourState _state;
    Transform _currentTarget;
    float _timeWithoutSight;
    float _attackCooldown;
    float _patrolCooldown;
    float _visionTimer;
    int _currentPatrolIndex;
    Vector2 _currentPatrolGoal;
    BehaviourState _stateBeforeDeath;
    bool _agroWindupActive;

    int _agroTriggerHash;
    int _returnTriggerHash;
    int _deathFromPatrolHash;
    int _deathFromAttackHash;

    [Inject]
    public void Construct([InjectOptional] SignalBus bus = null)
    {
        _bus = bus;
    }

    void Awake()
    {
        if (_jumpController == null)
            _jumpController = GetComponent<AggressiveJumperJumpController>();
        if (_health == null)
            _health = GetComponentInChildren<Health>();
        if (_damageReceiver == null)
            _damageReceiver = GetComponentInChildren<EnemyDamageReceiver>();
        if (_visualRoot == null)
            _visualRoot = transform;
        if (_facade == null)
            _facade = GetComponent<EnemyFacade>();

        CacheAnimatorHashes();
    }

    void OnEnable()
    {
        _state = BehaviourState.Patrol;
        _stateBeforeDeath = BehaviourState.Patrol;
        _attackCooldown = 0f;
        _patrolCooldown = 0f;
        _timeWithoutSight = 0f;
        _visionTimer = 0f;
        _currentPatrolIndex = 0;
        _currentPatrolGoal = GetCurrentPatrolGoal();
        _agroWindupActive = false;

        if (_jumpController != null)
        {
            if (_config != null)
                _jumpController.Configure(_config);
            _jumpController.Landed += OnJumpLanded;
        }

        if (_animator != null && _deathFromPatrolHash != 0)
            _animator.SetBool(_deathFromPatrolHash, false);

        if (_damageReceiver != null)
            _damageReceiver.DamageApplied += OnDamageTaken;

        if (_health != null)
            _health.OnHealthChanged += OnHealthChanged;

        PublishStateSignal(false);
    }

    void OnDisable()
    {
        if (_damageReceiver != null)
            _damageReceiver.DamageApplied -= OnDamageTaken;

        if (_health != null)
            _health.OnHealthChanged -= OnHealthChanged;

        if (_jumpController != null)
            _jumpController.Landed -= OnJumpLanded;
    }

    void Update()
    {
        if (_config == null || _jumpController == null || _state == BehaviourState.Dead)
            return;

        UpdateVision();
        UpdateCooldowns();

        switch (_state)
        {
            case BehaviourState.Patrol:
                TickPatrol();
                break;
            case BehaviourState.Agro:
                TickAgro();
                break;
        }
    }

    void UpdateVision()
    {
        if (_vision == null || _state == BehaviourState.Dead)
            return;

        _visionTimer -= Time.deltaTime;
        if (_visionTimer > 0f)
            return;

        _visionTimer = Mathf.Max(0.02f, _config.visionRescanInterval);

        bool sawPlayer = _vision.TryGetClosestTarget(out var candidate);
        if (sawPlayer && candidate != null)
        {
            _currentTarget = candidate;
            _timeWithoutSight = 0f;

            if (_state != BehaviourState.Agro)
                EnterAgro();
        }
        else if (_currentTarget != null)
        {
            _timeWithoutSight += _visionTimer;
            if (_timeWithoutSight >= _config.forgetDelay)
                LoseTarget();
        }
    }

    void UpdateCooldowns()
    {
        if (_attackCooldown > 0f)
            _attackCooldown -= Time.deltaTime;

        if (_patrolCooldown > 0f)
            _patrolCooldown -= Time.deltaTime;
    }

    void TickPatrol()
    {
        if (_patrolPath == null || _patrolPath.Count == 0)
            return;

        _currentPatrolGoal = GetCurrentPatrolGoal();

        Vector2 currentPos = _jumpController != null
            ? (Vector2)_jumpController.transform.position
            : (Vector2)transform.position;

        float tolerance = Mathf.Max(0.01f, _config.patrolLandingTolerance);
        Vector2 toGoal = _currentPatrolGoal - currentPos;
        float distance = toGoal.magnitude;

        if (distance <= tolerance)
        {
            AdvancePatrolIndex();
            return;
        }

        if (!_jumpController.CanQueuePatrolJump)
            return;

        if (_patrolCooldown > 0f)
            return;

        float maxStep = Mathf.Max(0.1f, _config.patrolMaxStepDistance);
        Vector2 target = distance > maxStep
            ? currentPos + toGoal.normalized * maxStep
            : _currentPatrolGoal;

        if (_jumpController.TryPlanPatrolJump(target))
        {
            FaceDirection(target.x - transform.position.x);
            _patrolCooldown = Mathf.Max(0f, _config.patrolIdleBetweenJumps);
            Log($"Patrol jump queued -> point {_currentPatrolIndex}");
        }
    }

    void TickAgro()
    {
        if (_currentTarget == null)
            return;

        if (_agroWindupActive)
            return;

        Vector3 targetPos = _currentTarget.position;
        float dir = targetPos.x - transform.position.x;

        FaceDirection(dir);

        if (!_jumpController.CanQueueAttackJump)
            return;

        if (_attackCooldown > 0f)
            return;

        Vector2 predicted = targetPos;
        float lead = Mathf.Sign(dir) * _config.attackLeadDistance;
        predicted.x += lead;

        if (_jumpController.TryPlanAttackJump(predicted))
        {
            _attackCooldown = Mathf.Max(0f, _config.attackCooldown);
            Log("Attack jump queued");
        }
    }

    void EnterAgro()
    {
        if (_state == BehaviourState.Dead)
            return;

        if (_state != BehaviourState.Agro)
        {
            _state = BehaviourState.Agro;
            _agroWindupActive = true;
            TriggerAnimator(_agroTriggerHash);
            Log("Enter agro");
            PublishStateSignal(true);
        }
    }

    void EnterPatrol()
    {
        if (_state == BehaviourState.Dead)
            return;

        if (_state != BehaviourState.Patrol)
        {
            _state = BehaviourState.Patrol;
            if (_returnTriggerHash != 0)
                TriggerAnimator(_returnTriggerHash);
            Log("Return to patrol");
            PublishStateSignal(false);
        }
    }

    void LoseTarget()
    {
        _currentTarget = null;
        _timeWithoutSight = 0f;
        _patrolCooldown = 0f;
        _agroWindupActive = false;
        _jumpController.CancelPendingJump();
        EnterPatrol();
    }

    void OnDamageTaken(DamageInfo info)
    {
        if (info == null)
            return;

        if (_state == BehaviourState.Dead)
            return;

        Vector2 hitPoint = info.source != null
            ? (Vector2)info.source.position
            : (info.hitPoint != Vector2.zero ? info.hitPoint : transform.position);

        FaceDirection(hitPoint.x - transform.position.x);

        if (info.source != null)
            _currentTarget = info.source;

        _timeWithoutSight = 0f;
        EnterAgro();
    }

    void OnHealthChanged()
    {
        if (_health == null || _health.IsAlive || _state == BehaviourState.Dead)
            return;

        _stateBeforeDeath = _state;
        _state = BehaviourState.Dead;
        _jumpController.CancelPendingJump();

        if (_deathFromPatrolHash != 0 && _stateBeforeDeath == BehaviourState.Patrol)
            SetBool(_deathFromPatrolHash, true);
        else if (_deathFromAttackHash != 0 && _stateBeforeDeath == BehaviourState.Agro)
            TriggerAnimator(_deathFromAttackHash);

        _agroWindupActive = false;
        PublishStateSignal(false);
        Log("Enemy died");
    }

    void FaceDirection(float dx)
    {
        if (_visualRoot == null || Mathf.Abs(dx) < 0.001f)
            return;

        float sign = Mathf.Sign(dx);
        Vector3 scale = _visualRoot.localScale;
        scale.x = Mathf.Abs(scale.x) * sign;
        _visualRoot.localScale = scale;
    }

    void CacheAnimatorHashes()
    {
        _agroTriggerHash = AnimatorStringToHash(_agroTrigger);
        _returnTriggerHash = AnimatorStringToHash(_returnToPatrolTrigger);
        _deathFromPatrolHash = AnimatorStringToHash(_deathFromPatrolBool);
        _deathFromAttackHash = AnimatorStringToHash(_deathFromAttackTrigger);
    }

    int AnimatorStringToHash(string param)
    {
        return string.IsNullOrEmpty(param) ? 0 : Animator.StringToHash(param);
    }

    void TriggerAnimator(int hash)
    {
        if (_animator == null || hash == 0)
            return;

        _animator.SetTrigger(hash);
    }

    void SetBool(int hash, bool value)
    {
        if (_animator == null || hash == 0)
            return;

        _animator.SetBool(hash, value);
    }

    void Log(string message)
    {
        if (_config != null && _config.verboseLogs)
            Debug.Log($"[AggressiveJumper] {message}", this);
    }

    void PublishStateSignal(bool agro)
    {
        if (_bus == null)
            return;

        _bus.Fire(new AggressiveJumperStateSignal
        {
            enemy = _facade,
            agro = agro
        });
    }

    void OnJumpLanded(AggressiveJumperJumpController.LandingInfo info)
    {
        if (info.type != AggressiveJumperJumpController.PendingJumpType.Patrol)
            return;

        if (_patrolPath == null || _patrolPath.Count == 0)
            return;

        Vector2 goal = GetCurrentPatrolGoal();

        float tolerance = Mathf.Max(0.01f, _config.patrolLandingTolerance);
        Vector2 desired = goal - info.start;
        Vector2 traveled = info.position - info.start;
        float desiredLength = desired.magnitude;
        float distanceToGoal = Vector2.Distance(info.position, goal);
        bool withinTolerance = distanceToGoal <= tolerance;
        bool passedGoal = false;

        if (desiredLength > 0.0001f)
        {
            float projected = Vector2.Dot(traveled, desired.normalized);
            passedGoal = projected >= desiredLength;
        }

        if (desiredLength < 0.0001f || withinTolerance || passedGoal)
            AdvancePatrolIndex();
    }

    void AdvancePatrolIndex()
    {
        if (_patrolPath == null || _patrolPath.Count == 0)
            return;

        if (_patrolPath.Count <= 1)
            _currentPatrolIndex = 0;
        else
            _currentPatrolIndex = (_currentPatrolIndex + 1) % _patrolPath.Count;

        _currentPatrolGoal = GetCurrentPatrolGoal();
    }

    Vector2 GetCurrentPatrolGoal()
    {
        if (_patrolPath == null || _patrolPath.Count == 0)
            return transform.position;

        Vector3 p = _patrolPath.GetPoint(_currentPatrolIndex);
        return new Vector2(p.x, p.y);
    }

    #region Animation Events

    // Called by Animation Event at the end of attack clip to smoothly go back to patrol tree
    public void AnimationEvent_RequestReturnToPatrol()
    {
        if (_state != BehaviourState.Dead)
            EnterPatrol();
    }

    public void AnimationEvent_OnAgroReady()
    {
        _agroWindupActive = false;
    }

    #endregion
}
