using System;
using UnityEngine;

[DisallowMultipleComponent]
public class AggressiveJumperJumpController : MonoBehaviour
{
    public enum PendingJumpType
    {
        None,
        Patrol,
        Attack
    }

    public struct LandingInfo
    {
        public PendingJumpType type;
        public Vector2 start;
        public Vector2 target;
        public Vector2 position;
    }

    [Header("Config")]
    [SerializeField] AggressiveJumperConfigSO _config;

    [Header("Refs")]
    [SerializeField] Rigidbody2D _rb;
    [SerializeField] Transform _visualRoot;

    PendingJumpType _pendingType;
    Vector2 _pendingTarget;
    bool _useMaxStep;
    float _maxStepDistance;
    Vector2 _lastJumpStart;
    float _postJumpTimer;
    bool _wasGrounded;
    PendingJumpType _lastJumpType;
    Vector2 _lastJumpTarget;

    public bool IsGrounded { get; private set; }
    public bool HasPendingJump => _pendingType != PendingJumpType.None;
    public bool MovementLockActive => _postJumpTimer > 0f;
    public event Action<LandingInfo> Landed;

    public void Configure(AggressiveJumperConfigSO config)
    {
        _config = config;
        _wasGrounded = IsGrounded;
    }

    void Reset()
    {
        if (_rb == null)
            _rb = GetComponent<Rigidbody2D>();
        if (_visualRoot == null)
            _visualRoot = transform;
    }

    void Awake()
    {
        if (_rb == null)
            _rb = GetComponent<Rigidbody2D>();
        if (_visualRoot == null)
            _visualRoot = transform;
    }

    void Update()
    {
        if (_postJumpTimer > 0f)
            _postJumpTimer -= Time.deltaTime;
    }

    void FixedUpdate()
    {
        UpdateGrounded();
    }

    public bool CanQueuePatrolJump => !HasPendingJump && !MovementLockActive && IsGrounded;
    public bool CanQueueAttackJump => !HasPendingJump && !MovementLockActive && IsGrounded;

    public bool TryPlanPatrolJump(Vector2 target)
    {
        if (_config == null || !CanQueuePatrolJump)
            return false;

        _pendingTarget = target;
        _useMaxStep = false;
        _maxStepDistance = 0f;
        _pendingType = PendingJumpType.Patrol;
        return true;
    }

    public bool TryPlanAttackJump(Vector2 target)
    {
        if (_config == null || !CanQueueAttackJump)
            return false;

        _pendingTarget = target;
        _useMaxStep = false;
        _maxStepDistance = 0f;
        _pendingType = PendingJumpType.Attack;
        return true;
    }
    
    public bool TryPlanAttackJumpSegment(Vector2 currentPosition, Vector2 goal, float maxStepDistance)
    {
        if (_config == null || !CanQueueAttackJump)
            return false;

        _pendingType = PendingJumpType.Attack;
        _pendingTarget = goal;
        _useMaxStep = true;
        _maxStepDistance = Mathf.Max(0.1f, maxStepDistance);
        _lastJumpStart = currentPosition;
        return true;
    }

    public void CancelPendingJump()
    {
        _pendingType = PendingJumpType.None;
        _useMaxStep = false;
        _maxStepDistance = 0f;
        _lastJumpType = PendingJumpType.None;
        _lastJumpStart = Vector2.zero;
        _lastJumpTarget = Vector2.zero;
    }

    public void StopImmediate()
    {
        CancelPendingJump();
        _postJumpTimer = 0f;

        if (_rb != null)
            _rb.linearVelocity = Vector2.zero;
    }

    public void AnimationEvent_PatrolJump()
    {
        if (_pendingType != PendingJumpType.Patrol)
            return;

        ExecuteJump(_config.patrolJump);
    }

    public void AnimationEvent_AttackJump()
    {
        if (_pendingType != PendingJumpType.Attack)
            return;

        ExecuteJump(_config.attackJump);
    }

    void ExecuteJump(AggressiveJumperConfigSO.JumpProfile profile)
    {
        if (_rb == null)
            return;

        Vector2 currentPos = _rb.position;
        PendingJumpType executedType = _pendingType;
        Vector2 goal = _pendingTarget;
        Vector2 target = goal;

        if (_useMaxStep && _maxStepDistance > 0.1f)
        {
            Vector2 toGoal = goal - currentPos;
            float distance = toGoal.magnitude;
            if (distance > _maxStepDistance)
                target = currentPos + toGoal.normalized * _maxStepDistance;
        }

        _lastJumpStart = currentPos;
        float gravity = GetEffectiveGravity();
        float airTime = ResolveAirTime(profile, gravity);
        Vector2 displacement = target - currentPos;
        float horizontalSign = Mathf.Sign(displacement.x == 0f ? (_visualRoot != null ? _visualRoot.localScale.x : 1f) : displacement.x);
        if (Mathf.Abs(horizontalSign) < 0.001f)
            horizontalSign = 1f;

        float vx = airTime > 0.001f ? displacement.x / airTime : 0f;
        float minHorizontalSpeed = Mathf.Abs(profile.horizontalVelocity);
        if (minHorizontalSpeed > 0.01f)
        {
            if (Mathf.Abs(vx) < minHorizontalSpeed)
                vx = minHorizontalSpeed * horizontalSign;
        }
        float vy;

        if (gravity > 0.0001f && airTime > 0.001f)
        {
            // vy * t - 0.5 * g * t^2 = displacement.y
            vy = (displacement.y + 0.5f * gravity * airTime * airTime) / airTime;
        }
        else
        {
            vy = profile.verticalVelocity;
        }

        _rb.linearVelocity = new Vector2(vx, vy);

        if (profile.impulse.sqrMagnitude > 0.0001f)
        {
            Vector2 impulse = new Vector2(horizontalSign * profile.impulse.x, profile.impulse.y);
            _rb.AddForce(impulse, ForceMode2D.Impulse);
        }

        _lastJumpType = executedType;
        _lastJumpTarget = target;

        _pendingType = PendingJumpType.None;
        _postJumpTimer = profile.postJumpDelay;
    }

    void UpdateGrounded()
    {
        if (_config == null)
        {
            IsGrounded = false;
            return;
        }

        Vector2 origin = (Vector2)transform.position + _config.groundCheckOffset;
        IsGrounded = Physics2D.OverlapCircle(origin, _config.groundCheckRadius, _config.groundMask);

        if (!_wasGrounded && IsGrounded && _lastJumpType != PendingJumpType.None)
            NotifyLanding();

        _wasGrounded = IsGrounded;
    }

    void NotifyLanding()
    {
        Vector2 landingPos = _rb != null ? _rb.position : (Vector2)transform.position;

        Landed?.Invoke(new LandingInfo
        {
            type = _lastJumpType,
            start = _lastJumpStart,
            target = _lastJumpTarget,
            position = landingPos
        });

        _lastJumpType = PendingJumpType.None;
        _lastJumpStart = Vector2.zero;
        _lastJumpTarget = Vector2.zero;
    }

    float GetEffectiveGravity()
    {
        float gravity = Mathf.Abs(Physics2D.gravity.y);
        if (_rb != null)
            gravity *= Mathf.Abs(_rb.gravityScale);
        return gravity;
    }

    float ResolveAirTime(AggressiveJumperConfigSO.JumpProfile profile, float gravity)
    {
        if (profile.airTime > 0.01f)
            return profile.airTime;

        if (gravity <= 0.0001f || profile.verticalVelocity <= 0.0001f)
            return Mathf.Max(profile.airTime, 0.2f);

        return Mathf.Max(0.05f, 2f * profile.verticalVelocity / gravity);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (_config == null)
            return;

        Vector2 origin = (Vector2)transform.position + _config.groundCheckOffset;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(origin, _config.groundCheckRadius);
    }
#endif
}
