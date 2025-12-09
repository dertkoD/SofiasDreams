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

    [Header("Config")]
    [SerializeField] AggressiveJumperConfigSO _config;

    [Header("Refs")]
    [SerializeField] Rigidbody2D _rb;
    [SerializeField] Transform _visualRoot;

    PendingJumpType _pendingType;
    Vector2 _pendingTarget;
    float _postJumpTimer;

    public bool IsGrounded { get; private set; }
    public bool HasPendingJump => _pendingType != PendingJumpType.None;
    public bool MovementLockActive => _postJumpTimer > 0f;

    public void Configure(AggressiveJumperConfigSO config)
    {
        _config = config;
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
        _pendingType = PendingJumpType.Patrol;
        return true;
    }

    public bool TryPlanAttackJump(Vector2 target)
    {
        if (_config == null || !CanQueueAttackJump)
            return false;

        _pendingTarget = target;
        _pendingType = PendingJumpType.Attack;
        return true;
    }

    public void CancelPendingJump()
    {
        _pendingType = PendingJumpType.None;
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

        Vector2 pos = _rb.position;
        float dir = Mathf.Sign(_pendingTarget.x - pos.x);
        if (Mathf.Abs(dir) < 0.001f)
            dir = Mathf.Sign(_visualRoot != null ? _visualRoot.localScale.x : transform.localScale.x);
        if (Mathf.Abs(dir) < 0.001f)
            dir = 1f;

        Vector2 velocity = new Vector2(
            dir * profile.horizontalVelocity,
            profile.verticalVelocity);

        _rb.linearVelocity = velocity;

        if (profile.impulse.sqrMagnitude > 0.0001f)
        {
            Vector2 impulse = new Vector2(dir * profile.impulse.x, profile.impulse.y);
            _rb.AddForce(impulse, ForceMode2D.Impulse);
        }

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
