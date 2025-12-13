using UnityEngine;
using Zenject;

/// <summary>
/// Ground checker implemented like player Jumper2D:
/// Rigidbody2D.IsTouching(ContactFilter2D) with normal-angle filter,
/// plus "leaveGroundVelocity" to avoid 1-frame stale grounded after jump starts.
/// </summary>
public class JumpingEnemyGroundChecker2D : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] Rigidbody2D _rb;

    [Header("Config overrides (optional, prefer SO via DI)")]
    [SerializeField] LayerMask _groundMaskOverride = ~0;

    JumpingEnemyConfigSO _config;
    bool _grounded;
    bool _isJumping;

    public bool IsGrounded => _grounded;

    [Inject]
    public void Construct(JumpingEnemyConfigSO config)
    {
        _config = config;
    }

    void Reset()
    {
        _rb = GetComponentInParent<Rigidbody2D>();
    }

    void Awake()
    {
        if (!_rb) _rb = GetComponentInParent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        _grounded = ComputeGrounded();
        if (_grounded) _isJumping = false;
    }

    /// <summary>Call when jump starts to prevent same-frame stale grounded state.</summary>
    public void NotifyJumpStarted()
    {
        _grounded = false;
        _isJumping = true;
    }

    bool ComputeGrounded()
    {
        if (!_rb) return false;

        LayerMask mask = (_config != null && _config.groundMask.value != 0) ? _config.groundMask : _groundMaskOverride;
        if (mask.value == 0) return false;

        float leaveVel = _config != null ? _config.leaveGroundVelocity : 0.05f;
        if (_isJumping && _rb.linearVelocity.y > leaveVel)
            return false;

        float minA = _config != null ? _config.minGroundNormalAngle : 80f;
        float maxA = _config != null ? _config.maxGroundNormalAngle : 100f;

        var filter = new ContactFilter2D
        {
            useTriggers = false,
            useLayerMask = true,
            layerMask = mask,
            useNormalAngle = true,
            minNormalAngle = minA,
            maxNormalAngle = maxA
        };

        return _rb.IsTouching(filter);
    }
}

