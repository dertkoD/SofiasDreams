using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class JumpingEnemyMotor2D : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] Rigidbody2D _rb;
    [SerializeField] Collider2D _bodyCollider;
    [SerializeField] Transform _facingTransform;

    [Header("Config (fallback if DI is not used)")]
    [SerializeField] JumpingEnemyConfigSO _configOverride;

    JumpingEnemyConfigSO _config;
    IMobilityGate _mobilityGate;
    IReadOnlyList<IHitStunState> _hitStunStates = Array.Empty<IHitStunState>();

    readonly ContactPoint2D[] _cpBuf = new ContactPoint2D[12];
    readonly RaycastHit2D[] _rhBuf = new RaycastHit2D[12];

    float _baseScaleX;
    bool _isGrounded;

    public Rigidbody2D Rigidbody => _rb;
    public bool IsGrounded => _isGrounded;
    public Vector2 Velocity => _rb ? _rb.linearVelocity : Vector2.zero;

    [Inject]
    public void Construct(
        [InjectOptional] JumpingEnemyConfigSO config = null,
        IMobilityGate mobilityGate,
        [InjectOptional] List<IHitStunState> hitStunStates = null)
    {
        _config = config != null ? config : _configOverride;
        _mobilityGate = mobilityGate;
        if (hitStunStates != null && hitStunStates.Count > 0)
            _hitStunStates = hitStunStates;
    }

    void Reset()
    {
        _rb = GetComponent<Rigidbody2D>();
        _bodyCollider = GetComponent<Collider2D>();
        _facingTransform = transform;
    }

    void Awake()
    {
        if (!_rb) _rb = GetComponent<Rigidbody2D>();
        if (!_bodyCollider) _bodyCollider = GetComponent<Collider2D>();
        if (!_facingTransform) _facingTransform = transform;
        if (_config == null) _config = _configOverride;

        _baseScaleX = Mathf.Abs(_facingTransform.localScale.x);
        if (_baseScaleX < 0.0001f) _baseScaleX = 1f;
    }

    void FixedUpdate()
    {
        _isGrounded = ComputeGrounded();
    }

    public void StopHorizontal()
    {
        if (!_rb) return;
        _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
    }

    public void Face(int sign)
    {
        sign = sign >= 0 ? +1 : -1;
        if (!_facingTransform) return;
        var s = _facingTransform.localScale;
        s.x = _baseScaleX * sign;
        _facingTransform.localScale = s;
    }

    public bool TryJump(int horizontalSign, float jumpHeight, float horizontalSpeed)
    {
        if (!_rb || _config == null) return false;
        if (!_isGrounded) return false;
        if (IsInHitStun()) return false;
        if (_mobilityGate != null && _mobilityGate.IsJumpBlocked) return false;

        horizontalSign = horizontalSign >= 0 ? +1 : -1;
        Face(horizontalSign);

        float g = Mathf.Abs(Physics2D.gravity.y * Mathf.Max(0f, _rb.gravityScale));
        float H = Mathf.Max(0f, jumpHeight);
        float vy0 = (g > 0f && H > 0f) ? Mathf.Sqrt(2f * g * H) : 0f;

        // keep behaviour stable: reset downward speed before impulse
        float vy = _rb.linearVelocity.y;
        if (vy < 0f) vy = 0f;

        _rb.linearVelocity = new Vector2(horizontalSign * Mathf.Max(0f, horizontalSpeed), vy0 + vy);
        return true;
    }

    bool ComputeGrounded()
    {
        if (_config == null || !_rb || !_bodyCollider || _config.groundMask.value == 0)
            return false;

        var filter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = _config.groundMask,
            useTriggers = false
        };

        int c = _rb.GetContacts(filter, _cpBuf);
        for (int i = 0; i < c; i++)
            if (_cpBuf[i].normal.y >= _config.minGroundNormalY) return true;

        float dist = Mathf.Max(0.001f, _config.groundCastDistance);
        int h = _bodyCollider.Cast(Vector2.down, filter, _rhBuf, dist);
        for (int i = 0; i < h; i++)
            if (_rhBuf[i].normal.y >= _config.minGroundNormalY) return true;

        return false;
    }

    bool IsInHitStun()
    {
        if (_hitStunStates == null) return false;
        for (int i = 0; i < _hitStunStates.Count; i++)
            if (_hitStunStates[i] != null && _hitStunStates[i].InHitStun) return true;
        return false;
    }
}

