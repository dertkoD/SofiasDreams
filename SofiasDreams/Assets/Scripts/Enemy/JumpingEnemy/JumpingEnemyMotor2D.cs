using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class JumpingEnemyMotor2D : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] Rigidbody2D _rb;
    [SerializeField] Transform _facingTransform;
    [SerializeField] JumpingEnemyGroundChecker2D _groundChecker;

    JumpingEnemyConfigSO _config;
    IMobilityGate _mobilityGate;
    IReadOnlyList<IHitStunState> _hitStunStates = Array.Empty<IHitStunState>();

    float _baseScaleX;
    bool _isGrounded;
    bool _frozen;
    RigidbodyConstraints2D _savedConstraints;
    
    // Air control (continuous X while in air)
    bool _airControlActive;
    float _airDesiredVX;

    public Rigidbody2D Rigidbody => _rb;
    public bool IsGrounded => _isGrounded;
    public Vector2 Velocity => _rb ? _rb.linearVelocity : Vector2.zero;
    public bool IsFrozen => _frozen;

    [Inject]
    public void Construct(
        JumpingEnemyConfigSO config,
        IMobilityGate mobilityGate,
        [InjectOptional] List<IHitStunState> hitStunStates = null)
    {
        _config = config;
        _mobilityGate = mobilityGate;
        if (hitStunStates != null && hitStunStates.Count > 0)
            _hitStunStates = hitStunStates;
    }

    void Reset()
    {
        _rb = GetComponent<Rigidbody2D>();
        _facingTransform = transform;
        _groundChecker = GetComponentInChildren<JumpingEnemyGroundChecker2D>(true);
    }

    void Awake()
    {
        if (!_rb) _rb = GetComponent<Rigidbody2D>();
        if (!_facingTransform) _facingTransform = transform;
        if (!_groundChecker) _groundChecker = GetComponentInChildren<JumpingEnemyGroundChecker2D>(true);

        _baseScaleX = Mathf.Abs(_facingTransform.localScale.x);
        if (_baseScaleX < 0.0001f) _baseScaleX = 1f;
        if (_rb) _savedConstraints = _rb.constraints;
    }

    void FixedUpdate()
    {
        if (_frozen && _rb)
        {
            _rb.linearVelocity = Vector2.zero;
            _rb.angularVelocity = 0f;
        }
        _isGrounded = _groundChecker != null && _groundChecker.IsGrounded;

        if (_isGrounded)
            _airControlActive = false;

        TickAirControl();
    }

    void TickAirControl()
    {
        if (!_airControlActive) return;
        if (_frozen) return;
        if (_rb == null) return;
        if (_config == null) return;
        if (_groundChecker != null && _groundChecker.IsGrounded) { _airControlActive = false; return; }
        if (IsInHitStun()) return;
        if (_mobilityGate != null && _mobilityGate.IsMovementBlocked) return;

        float accel = Mathf.Max(0f, _config.airControlAcceleration);
        float dt = Time.fixedDeltaTime;

        float vx = _rb.linearVelocity.x;
        float target = _airDesiredVX;

        float maxDelta = accel * dt;
        float newVX = Mathf.MoveTowards(vx, target, maxDelta);

        // Optional clamp per-tick (useful if you want very snappy control but still avoid teleport-like changes)
        if (_config.airControlMaxDeltaVX > 0f)
        {
            float clamp = _config.airControlMaxDeltaVX;
            newVX = Mathf.Clamp(newVX, vx - clamp, vx + clamp);
        }

        _rb.linearVelocity = new Vector2(newVX, _rb.linearVelocity.y);
    }

    public void StopHorizontal()
    {
        if (!_rb) return;
        _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
    }

    public void StopAll()
    {
        if (!_rb) return;
        _rb.linearVelocity = Vector2.zero;
        _rb.angularVelocity = 0f;
    }

    /// <summary>Полная блокировка физ. движения на время триггер-клипов.</summary>
    public void SetFrozen(bool frozen)
    {
        if (!_rb) return;
        if (_frozen == frozen) return;

        _frozen = frozen;
        if (frozen)
        {
            _savedConstraints = _rb.constraints;
            StopAll();
            _rb.constraints = RigidbodyConstraints2D.FreezeAll;
        }
        else
        {
            _rb.constraints = _savedConstraints;
        }
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
        if (_frozen) return false;
        if (_groundChecker == null) return false;
        if (!_groundChecker.IsGrounded) return false;
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

        // Set only Y at start. X is controlled continuously during the jump (air control),
        // so if X gets cancelled by a wall at takeoff it can recover once we clear the wall.
        var v0 = _rb.linearVelocity;
        v0.y = vy0 + vy;
        _rb.linearVelocity = v0;

        _airControlActive = true;
        _airDesiredVX = horizontalSign * Mathf.Max(0f, horizontalSpeed);

        // Important: immediately mark as not grounded (FixedUpdate will catch up next physics tick).
        _isGrounded = false;
        _groundChecker.NotifyJumpStarted();
        return true;
    }

    bool IsInHitStun()
    {
        if (_hitStunStates == null) return false;
        for (int i = 0; i < _hitStunStates.Count; i++)
            if (_hitStunStates[i] != null && _hitStunStates[i].InHitStun) return true;
        return false;
    }
}

