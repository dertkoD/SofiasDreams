using UnityEngine;
using Zenject;

public class AggressiveJumpEnemyBrain : MonoBehaviour
{
    enum Mode { Patrol, Aggro }

    [Header("Refs")]
    [SerializeField] EnemyFacade _facade;
    [SerializeField] AggressiveJumpEnemyMotor _motor;
    [SerializeField] VisionCone2D _vision;
    [SerializeField] EnemyPatrolPath _patrolPath;
    [SerializeField] Animator _animator;
    [SerializeField] EnemyDamageReceiver _damageReceiver;

    [Header("Animator params")]
    [SerializeField] string _agroTrigger = "Agro";
    [SerializeField] string _returnToPatrolTrigger = "Patrol";
    [SerializeField] string _deathFromPatrolTrigger = "DeathFromPatrol";
    [SerializeField] string _deathFromAttackTrigger = "DeathFromAttack";

    AggressiveJumpEnemyConfigSO _config;
    Health _health;

    int _agroHash;
    int _returnHash;
    int _deathPatrolHash;
    int _deathAttackHash;

    Mode _mode;
    bool _isDead;
    bool _configApplied;

    float _forgetTimer;
    Transform _currentTarget;
    Rigidbody2D _currentTargetBody;
    Vector2 _lastKnownTarget;
    bool _hasLastKnownTarget;

    int _patrolIndex;
    int _patrolDirection = 1;
    bool _patrolTargetDirty = true;

    [Inject]
    void Construct(EnemyConfigSO config)
    {
        _config = config as AggressiveJumpEnemyConfigSO;
        if (_config == null)
            Debug.LogError("[AggressiveJumpEnemyBrain] EnemyConfigSO is not AggressiveJumpEnemyConfigSO", this);
    }

    void Awake()
    {
        if (!_facade) _facade = GetComponentInParent<EnemyFacade>();
        if (!_motor) _motor = GetComponentInChildren<AggressiveJumpEnemyMotor>();
        if (!_vision) _vision = GetComponentInChildren<VisionCone2D>();
        if (!_animator) _animator = GetComponentInChildren<Animator>();
        if (!_damageReceiver) _damageReceiver = GetComponentInChildren<EnemyDamageReceiver>();
        if (!_patrolPath)
            _patrolPath = GetComponentInChildren<EnemyPatrolPath>();

        _health = _facade ? _facade.Health : GetComponentInChildren<Health>();

        _agroHash = Animator.StringToHash(_agroTrigger);
        _returnHash = Animator.StringToHash(_returnToPatrolTrigger);
        _deathPatrolHash = Animator.StringToHash(_deathFromPatrolTrigger);
        _deathAttackHash = Animator.StringToHash(_deathFromAttackTrigger);

        TryApplyConfig();
    }

    void OnEnable()
    {
        if (_health != null)
            _health.OnHealthChanged += OnHealthChanged;

        if (_damageReceiver != null)
            _damageReceiver.DamageTaken += OnDamageTaken;

        if (_motor != null)
            _motor.PatrolJumpPerformed += AdvancePatrolPoint;
    }

    void OnDisable()
    {
        if (_health != null)
            _health.OnHealthChanged -= OnHealthChanged;

        if (_damageReceiver != null)
            _damageReceiver.DamageTaken -= OnDamageTaken;

        if (_motor != null)
            _motor.PatrolJumpPerformed -= AdvancePatrolPoint;
    }

    void Start()
    {
        TryApplyConfig();
        EnterPatrol(instant: true);
    }

    void Update()
    {
        if (_isDead)
            return;

        TickVisionAndForgetTimer();

        if (_mode == Mode.Aggro)
            TickAggro();
        else
            TickPatrol();
    }

    void TickVisionAndForgetTimer()
    {
        bool seesTarget = false;
        Transform seenTransform = null;

        if (_vision != null)
            seesTarget = _vision.TryGetClosestTarget(out seenTransform);

        if (seesTarget && seenTransform != null)
        {
            AcquireTarget(seenTransform);
            _forgetTimer = _config ? _config.aggroForgetDelay : 2f;

            if (_mode != Mode.Aggro)
                EnterAggro();
        }
        else if (_mode == Mode.Aggro)
        {
            if (_forgetTimer > 0f)
                _forgetTimer -= Time.deltaTime;
            else
                ExitAggroToPatrol();
        }
    }

    void TickPatrol()
    {
        if (_patrolPath == null || _patrolPath.Count == 0 || _motor == null)
            return;

        if (_patrolTargetDirty)
            PushCurrentPatrolTarget();
    }

    void TickAggro()
    {
        if (_currentTarget != null)
        {
            _lastKnownTarget = _currentTarget.position;
            _hasLastKnownTarget = true;

            if (_currentTargetBody == null)
                _currentTargetBody = _currentTarget.GetComponentInParent<Rigidbody2D>();
        }

        if (!_hasLastKnownTarget || _motor == null)
            return;

        Vector2 predicted = _lastKnownTarget;
        if (_currentTargetBody != null)
            predicted += _currentTargetBody.linearVelocity * (_config ? _config.attackLeadTime : 0.1f);

        _motor.SetAttackTarget(predicted);
        _motor.FaceTowards(predicted);
    }

    void PushCurrentPatrolTarget()
    {
        if (_patrolPath == null || _patrolPath.Count == 0 || _motor == null)
            return;

        Vector2 target = _patrolPath.GetPoint(_patrolIndex);
        _motor.SetPatrolTarget(target);
        _motor.FaceTowards(target);

        _patrolTargetDirty = false;
    }

    void AdvancePatrolPoint()
    {
        if (_mode != Mode.Patrol || _patrolPath == null || _patrolPath.Count <= 1)
            return;

        if (_config != null && _config.loopPatrol)
        {
            _patrolIndex = (_patrolIndex + 1) % _patrolPath.Count;
        }
        else
        {
            int next = _patrolIndex + _patrolDirection;
            if (next >= _patrolPath.Count || next < 0)
            {
                _patrolDirection *= -1;
                next = Mathf.Clamp(_patrolIndex + _patrolDirection, 0, _patrolPath.Count - 1);
            }

            _patrolIndex = next;
        }

        _patrolTargetDirty = true;
    }

    void EnterPatrol(bool instant)
    {
        _mode = Mode.Patrol;
        _forgetTimer = 0f;
        ClearTarget();

        if (_motor != null)
        {
            _motor.ClearAttackTarget();
            _motor.FaceTowards(transform.position + Vector3.right);
        }

        _patrolTargetDirty = true;

        if (!instant && _animator)
            _animator.SetTrigger(_returnHash);
    }

    void EnterAggro()
    {
        if (_mode == Mode.Aggro)
            return;

        _mode = Mode.Aggro;
        if (_animator)
            _animator.SetTrigger(_agroHash);
    }

    void ExitAggroToPatrol()
    {
        if (_mode != Mode.Aggro)
            return;

        EnterPatrol(instant: false);
    }

    void AcquireTarget(Transform target)
    {
        if (!target)
            return;

        _currentTarget = target;
        _currentTargetBody = target.GetComponentInParent<Rigidbody2D>();
        _lastKnownTarget = target.position;
        _hasLastKnownTarget = true;

        if (_motor != null)
        {
            _motor.SetAttackTarget(target.position);
            _motor.FaceTowards(target.position);
        }
    }

    void ClearTarget()
    {
        _currentTarget = null;
        _currentTargetBody = null;
        _hasLastKnownTarget = false;
    }

    void OnDamageTaken(DamageInfo info)
    {
        if (_isDead)
            return;

        Vector2 lookPoint = transform.position;
        if (info != null)
        {
            Transform source = info.source ? info.source.root : null;
            if (source != null)
            {
                AcquireTarget(source);
                lookPoint = source.position;
            }
            else if (info.hitPoint != Vector2.zero)
            {
                _lastKnownTarget = info.hitPoint;
                _hasLastKnownTarget = true;
                lookPoint = info.hitPoint;
            }
        }

        if (_motor != null)
            _motor.FaceTowards(lookPoint);

        float aggroTime = _config ? _config.hitAggroDuration : 2f;

        if (_mode != Mode.Aggro)
        {
            _forgetTimer = aggroTime;
            EnterAggro();
        }
        else
        {
            _forgetTimer = Mathf.Max(_forgetTimer, aggroTime);
        }
    }

    void OnHealthChanged()
    {
        if (_health == null || _health.IsAlive || _isDead)
            return;

        _isDead = true;

        if (_motor != null)
            _motor.StopImmediately();

        int deathHash = _mode == Mode.Aggro ? _deathAttackHash : _deathPatrolHash;
        if (_animator)
            _animator.SetTrigger(deathHash);
    }

    void TryApplyConfig()
    {
        if (_configApplied)
            return;
        if (_config == null || _motor == null)
            return;

        _motor.Configure(_config);
        _configApplied = true;
    }
}
