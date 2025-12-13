using UnityEngine;
using Zenject;

/// <summary>
/// Robust ground checker for jumping enemy:
/// uses a small overlap shape under feet (no "near ground" false positives).
/// </summary>
public class JumpingEnemyGroundChecker2D : MonoBehaviour
{
    [Header("Config overrides (optional, prefer SO via DI)")]
    [SerializeField] LayerMask _groundMaskOverride;
    [SerializeField] bool _useOval = true;
    [SerializeField] Vector2 _ovalOffset = new(0f, -0.45f);
    [SerializeField] Vector2 _ovalSize = new(0.6f, 0.2f);
    [SerializeField] Vector2 _circleOffset = new(0f, -0.45f);
    [SerializeField] float _circleRadius = 0.15f;

    JumpingEnemyConfigSO _config;
    bool _grounded;
    float _lastUngroundedAt;

    public bool IsGrounded => _grounded;

    [Inject]
    public void Construct(JumpingEnemyConfigSO config)
    {
        _config = config;
    }

    void FixedUpdate()
    {
        _grounded = ComputeGrounded();
        if (!_grounded) _lastUngroundedAt = Time.time;
    }

    /// <summary>Call when jump starts to prevent same-frame stale grounded state.</summary>
    public void NotifyJumpStarted()
    {
        _grounded = false;
        _lastUngroundedAt = Time.time;
    }

    bool ComputeGrounded()
    {
        LayerMask mask = _config != null && _config.groundMask.value != 0 ? _config.groundMask : _groundMaskOverride;
        if (mask.value == 0) return false;

        bool useOval = _config != null ? _config.useOvalGroundCheck : _useOval;

        if (useOval)
        {
            Vector2 offset = _config != null ? _config.groundCheckOvalOffset : _ovalOffset;
            Vector2 size = _config != null ? _config.groundCheckOvalSize : _ovalSize;
            Vector2 p = (Vector2)transform.position + offset;
            return Physics2D.OverlapCapsule(
                p,
                new Vector2(Mathf.Max(0.001f, size.x), Mathf.Max(0.001f, size.y)),
                CapsuleDirection2D.Horizontal,
                0f,
                mask
            );
        }
        else
        {
            Vector2 offset = _config != null ? _config.groundCheckOffset : _circleOffset;
            float r = _config != null ? _config.groundCheckRadius : _circleRadius;
            Vector2 p = (Vector2)transform.position + offset;
            return Physics2D.OverlapCircle(p, Mathf.Max(0.001f, r), mask);
        }
    }
}

