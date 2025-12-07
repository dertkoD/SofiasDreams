using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class EnemyMovement : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] Rigidbody2D _rb;

    EnemyConfigSO _config;
    IMobilityGate _mobilityGate;
    IReadOnlyList<IHitStunState> _hitStunStates = Array.Empty<IHitStunState>();

    float _targetX;
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
    }

    public void MoveTo(Vector3 worldPos)
    {
        _targetX = worldPos.x;
        _hasTarget = true;
    }

    public void Stop()
    {
        _hasTarget = false;

        if (_rb != null)
        {
            _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
            _velocity = _rb.linearVelocity;
        }
        else
        {
            _velocity = Vector2.zero;
        }
    }
    
    public bool IsAtDestination(float tolerance)
    {
        if (!_hasTarget)
            return true;

        float t = Mathf.Max(tolerance, 0.001f);
        float x = _rb != null ? _rb.position.x : transform.position.x;
        float dx = _targetX - x;

        return Mathf.Abs(dx) <= t;
    }

    public void WarpTo(Vector3 worldPos)
    {
        if (_rb != null)
            _rb.position = worldPos;
        else
            transform.position = worldPos;

        _targetX = worldPos.x;
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
            _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
            _velocity = _rb.linearVelocity;
            ApplyFlip();
            return;
        }

        if (!_hasTarget)
        {
            _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
            _velocity = _rb.linearVelocity;
            ApplyFlip();
            return;
        }

        float x  = _rb.position.x;
        float dx = _targetX - x;

        if (Mathf.Abs(dx) <= _config.destinationTolerance)
        {
            _hasTarget = false;
            _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
            _velocity = _rb.linearVelocity;
            ApplyFlip();
            return;
        }

        float dir = Mathf.Sign(dx);
        float vx  = dir * _config.moveSpeed;
        float vy  = _rb.linearVelocity.y;

        _rb.linearVelocity = new Vector2(vx, vy);
        _velocity = _rb.linearVelocity;

        ApplyFlip();
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
