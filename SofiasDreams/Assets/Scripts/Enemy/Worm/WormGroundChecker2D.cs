using UnityEngine;
using Zenject;

public class WormGroundChecker2D : MonoBehaviour, IEnemyGroundChecker
{
    [Header("Refs")]
    [SerializeField] Rigidbody2D _rb;
    [SerializeField] LayerMask _fallbackGroundMask;

    WormConfigSO _config;
    bool _grounded;
    bool _isBouncing;

    public bool IsGrounded => _grounded;

    [Inject]
    public void Construct(WormConfigSO config)
    {
        _config = config;
    }

    void Awake()
    {
        if (!_rb) _rb = GetComponentInParent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        _grounded = ComputeGrounded();
        if (_grounded) _isBouncing = false;
    }

    public void NotifyBounceStarted()
    {
        _grounded = false;
        _isBouncing = true;
    }

    bool ComputeGrounded()
    {
        if (!_rb) return false;

        LayerMask mask = (_config != null && _config.groundMask.value != 0) ? _config.groundMask : _fallbackGroundMask;
        if (mask.value == 0) return false;

        // If moving up quickly, assume not grounded (to exit ground snap)
        float leaveVel = _config != null ? _config.leaveGroundVelocity : 0.1f;
        if (_isBouncing && _rb.linearVelocity.y > leaveVel)
            return false;

        float minA = _config != null ? _config.minGroundNormalAngle : 60f;
        float maxA = _config != null ? _config.maxGroundNormalAngle : 120f;

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
