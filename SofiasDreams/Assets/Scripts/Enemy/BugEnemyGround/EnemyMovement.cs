using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class EnemyMovement : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] Rigidbody2D _rb;

    [Header("Mode")]
    [SerializeField] EnemyMovementMode _movementMode = EnemyMovementMode.GroundOnly;

    EnemyConfigSO _config;
    IMobilityGate _mobilityGate;
    IReadOnlyList<IHitStunState> _hitStunStates = Array.Empty<IHitStunState>();

    float _targetX;
    Vector2 _targetPosition;
    bool _hasTarget;

    Vector2 _velocity;

    float _baseScaleX;

    public Vector3 Position => transform.position;
    public Vector3 Velocity => _velocity;

    [Inject]
    public void Construct(IMobilityGate mobilityGate, [InjectOptional] List<IHitStunState> hitStunStates = null)
    {
        _mobilityGate = mobilityGate;
        if (hitStunStates != null && hitStunStates.Count > 0)
            _hitStunStates = hitStunStates;
    }

    public void Configure(EnemyConfigSO config)
    {
        _config = config;

        if (_config != null)
            _movementMode = _config.movementMode;
    }

    public EnemyMovementMode MovementMode => _movementMode;

    public void SetMovementMode(EnemyMovementMode mode)
    {
        _movementMode = mode;
    }

    public void MoveTo(Vector3 worldPos)
    {
        if (_movementMode == EnemyMovementMode.GroundOnly)
            _targetX = worldPos.x;
        else
            _targetPosition = new Vector2(worldPos.x, worldPos.y);

        _hasTarget = true;
    }

    public void Stop()
    {
        _hasTarget = false;

        HaltVelocity();
    }
    
    public bool IsAtDestination(float tolerance)
    {
        if (!_hasTarget)
            return true;

        float t = Mathf.Max(tolerance, 0.001f);

        if (_movementMode == EnemyMovementMode.GroundOnly)
        {
            float x = _rb != null ? _rb.position.x : transform.position.x;
            float dx = _targetX - x;
            return Mathf.Abs(dx) <= t;
        }

        Vector2 current = _rb != null ? _rb.position : (Vector2)transform.position;
        float distance = Vector2.Distance(_targetPosition, current);
        return distance <= t;
    }

    public void WarpTo(Vector3 worldPos)
    {
        if (_rb != null)
            _rb.position = worldPos;
        else
            transform.position = worldPos;

        _targetX = worldPos.x;
        _targetPosition = new Vector2(worldPos.x, worldPos.y);
    }

    void Reset()
    {
        if (_rb == null)
            _rb = GetComponent<Rigidbody2D>();
    }

    void Awake()
    {
        if (!_rb) _rb = GetComponent<Rigidbody2D>();

        _baseScaleX = Mathf.Abs(transform.localScale.x);
        if (_baseScaleX < 0.0001f) _baseScaleX = 1f;

        if (_rb != null)
        {
            _targetX = _rb.position.x;
            _targetPosition = _rb.position;
        }
        else
        {
            _targetX = transform.position.x;
            _targetPosition = transform.position;
        }
    }

    void FixedUpdate()
    {
        if (_rb == null || _config == null)
            return;

        if (IsInHitStun())
        {
            _velocity = _rb.linearVelocity;
            return;
        }

        if (_mobilityGate != null && _mobilityGate.IsMovementBlocked)
        {
            HaltVelocity();
            ApplyFlip();
            return;
        }

        if (!_hasTarget)
        {
            HaltVelocity();
            ApplyFlip();
            return;
        }

        if (_movementMode == EnemyMovementMode.GroundOnly)
            TickHorizontalMovement();
        else
            TickPlanarMovement();

        ApplyFlip();
    }

    void TickHorizontalMovement()
    {
        float x = _rb.position.x;
        float dx = _targetX - x;

        if (Mathf.Abs(dx) <= _config.destinationTolerance)
        {
            _hasTarget = false;
            HaltVelocity();
            return;
        }

        float dir = Mathf.Sign(dx);
        float vx = dir * _config.moveSpeed;
        float vy = _rb.linearVelocity.y;

        _rb.linearVelocity = new Vector2(vx, vy);
        _velocity = _rb.linearVelocity;
    }

    void TickPlanarMovement()
    {
        Vector2 current = _rb.position;
        Vector2 delta = _targetPosition - current;

        float tolerance = Mathf.Max(_config.destinationTolerance, 0.001f);
        if (delta.sqrMagnitude <= tolerance * tolerance)
        {
            _hasTarget = false;
            HaltVelocity();
            return;
        }

        Vector2 dir = delta.normalized;
        Vector2 velocity = dir * _config.moveSpeed;

        _rb.linearVelocity = velocity;
        _velocity = _rb.linearVelocity;
    }

    void HaltVelocity()
    {
        if (_rb != null)
        {
        if (_movementMode == EnemyMovementMode.GroundOnly)
                _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
            else
                _rb.linearVelocity = Vector2.zero;

            _velocity = _rb.linearVelocity;
        }
        else
        {
            _velocity = Vector2.zero;
        }
    }

    void ApplyFlip()
    {
        if (Mathf.Abs(_velocity.x) < 0.01f)
            return;

        float sign = Mathf.Sign(_velocity.x);
        var scale = transform.localScale;
        scale.x = _baseScaleX * sign;
        transform.localScale = scale;
    }

    bool IsInHitStun()
    {
        if (_hitStunStates == null)
            return false;

        for (int i = 0; i < _hitStunStates.Count; i++)
        {
            if (_hitStunStates[i] != null && _hitStunStates[i].InHitStun)
                return true;
        }

        return false;
    }
}
