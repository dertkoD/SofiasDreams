using System;
using UnityEngine;
using Zenject;

public class AggressiveJumpEnemyMotor : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] Rigidbody2D _rb;
    [SerializeField] Transform _facingRoot;
    [SerializeField] Collider2D _groundCollider;
    [SerializeField] Transform _groundProbePivot;

    [Header("Flip")]
    [SerializeField] float _minFlipVelocity = 0.05f;

    AggressiveJumpEnemyConfigSO _config;
    IMobilityGate _mobilityGate;

    Vector2 _patrolTarget;
    Vector2 _attackTarget;
    bool _hasPatrolTarget;
    bool _hasAttackTarget;

    float _lastPatrolJumpTime = -999f;
    float _lastAttackJumpTime = -999f;
    float _baseScaleX = 1f;

    public event Action PatrolJumpPerformed;
    public event Action AttackJumpPerformed;

    public bool IsGrounded => CheckGround();
    public Vector2 Velocity => _rb ? _rb.linearVelocity : Vector2.zero;

    [Inject]
    public void Construct([InjectOptional] IMobilityGate mobilityGate = null)
    {
        _mobilityGate = mobilityGate;
    }

    public void Configure(AggressiveJumpEnemyConfigSO config)
    {
        _config = config;
    }

    void Awake()
    {
        if (!_rb) _rb = GetComponent<Rigidbody2D>();
        if (!_facingRoot) _facingRoot = transform;
        if (!_groundCollider) _groundCollider = GetComponentInChildren<Collider2D>();

        _baseScaleX = Mathf.Abs(_facingRoot.localScale.x);
        if (_baseScaleX < 0.0001f) _baseScaleX = 1f;
    }

    public void SetPatrolTarget(Vector2 worldPos)
    {
        _patrolTarget = worldPos;
        _hasPatrolTarget = true;
    }

    public void SetAttackTarget(Vector2 worldPos)
    {
        _attackTarget = worldPos;
        _hasAttackTarget = true;
    }

    public void ClearAttackTarget()
    {
        _hasAttackTarget = false;
    }

    public void FaceTowards(Vector2 worldPos)
    {
        float dir = Mathf.Sign(worldPos.x - CurrentX());
        if (Mathf.Abs(dir) < 0.001f)
            dir = Mathf.Sign(_facingRoot.localScale.x);
        FaceDirection(dir);
    }

    public void FaceDirection(float directionSign)
    {
        if (!_facingRoot)
            return;

        float sign = Mathf.Sign(directionSign);
        if (Mathf.Abs(sign) < 0.001f)
            sign = 1f;

        var scale = _facingRoot.localScale;
        scale.x = _baseScaleX * sign;
        _facingRoot.localScale = scale;
    }

    public void StopImmediately()
    {
        if (_rb)
            _rb.linearVelocity = Vector2.zero;
    }

    public bool PerformPatrolJump()
    {
        if (_config == null)
            return false;

        if (!IsJumpReady(_lastPatrolJumpTime, _config.patrolJumpCooldown))
            return false;

        float dir = GetDirectionSign(_hasPatrolTarget ? _patrolTarget : (Vector2)transform.position + Vector2.right);
        ApplyJump(_config.patrolJumpHorizontalSpeed, _config.patrolJumpUpVelocity, dir);

        _lastPatrolJumpTime = Time.time;
        PatrolJumpPerformed?.Invoke();
        return true;
    }

    public bool PerformAttackJump()
    {
        if (_config == null)
            return false;

        if (!IsJumpReady(_lastAttackJumpTime, _config.aggroJumpCooldown))
            return false;

        Vector2 fallback = (Vector2)transform.position + (Vector2.right * FacingSign());
        float dir = GetDirectionSign(_hasAttackTarget ? _attackTarget : fallback);

        ApplyJump(_config.aggroJumpHorizontalSpeed, _config.aggroJumpUpVelocity, dir);

        _lastAttackJumpTime = Time.time;
        AttackJumpPerformed?.Invoke();
        return true;
    }

    bool IsJumpReady(float lastJumpTime, float cooldown)
    {
        if (_rb == null)
            return false;
        if (_mobilityGate != null && (_mobilityGate.IsMovementBlocked || _mobilityGate.IsJumpBlocked))
            return false;
        if (!IsGrounded)
            return false;
        return Time.time >= lastJumpTime + Mathf.Max(0f, cooldown);
    }

    void ApplyJump(float horizontalSpeed, float verticalVelocity, float directionSign)
    {
        if (_rb == null)
            return;

        float dir = Mathf.Abs(directionSign) < 0.001f ? 1f : Mathf.Sign(directionSign);
        Vector2 velocity = new Vector2(horizontalSpeed * dir, verticalVelocity);
        _rb.linearVelocity = velocity;

        if (Mathf.Abs(velocity.x) >= _minFlipVelocity)
            FaceDirection(dir);
    }

    float GetDirectionSign(Vector2 target)
    {
        float dx = target.x - CurrentX();
        if (Mathf.Abs(dx) < 0.001f)
            dx = FacingSign();
        return Mathf.Sign(dx);
    }

    float CurrentX()
    {
        if (_rb)
            return _rb.position.x;
        return transform.position.x;
    }

    bool CheckGround()
    {
        Vector2 origin;
        if (_groundProbePivot)
            origin = _groundProbePivot.position;
        else if (_groundCollider)
            origin = new Vector2(_groundCollider.bounds.center.x, _groundCollider.bounds.min.y);
        else if (_rb)
            origin = _rb.position;
        else
            origin = transform.position;

        float radius = _config ? Mathf.Max(0.01f, _config.groundProbeRadius) : 0.1f;
        float distance = _config ? Mathf.Max(0.01f, _config.groundProbeDistance) : 0.05f;
        LayerMask mask = _config ? _config.groundMask : Physics2D.DefaultRaycastLayers;

        var hit = Physics2D.CircleCast(origin, radius, Vector2.down, distance, mask);
        return hit.collider != null;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (_config == null)
            return;

        Gizmos.color = Color.red;
        Vector2 origin = _groundProbePivot
            ? (Vector2)_groundProbePivot.position
            : (_groundCollider ? new Vector2(_groundCollider.bounds.center.x, _groundCollider.bounds.min.y) : (Vector2)transform.position);
        Gizmos.DrawWireSphere(origin - Vector2.up * Mathf.Max(0.01f, _config.groundProbeDistance), Mathf.Max(0.01f, _config.groundProbeRadius));
    }
#endif

    float FacingSign()
    {
        if (!_facingRoot)
            return 1f;
        float sign = Mathf.Sign(_facingRoot.localScale.x);
        return Mathf.Abs(sign) < 0.001f ? 1f : sign;
    }
}
