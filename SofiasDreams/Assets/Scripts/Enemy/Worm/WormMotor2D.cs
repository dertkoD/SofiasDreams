using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class WormMotor2D : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] Rigidbody2D _rb;
    [SerializeField] Transform _facingTransform;
    [SerializeField] LedgeGuard2D _ledgeGuard;
    [SerializeField] WormGroundChecker2D _groundChecker;

    WormConfigSO _config;
    IReadOnlyList<IHitStunState> _hitStunStates = Array.Empty<IHitStunState>();
    
    bool _frozen;
    float _baseScaleX;
    RigidbodyConstraints2D _savedConstraints;

    public Rigidbody2D Rigidbody => _rb;
    public bool IsFrozen => _frozen;
    public bool IsGrounded => _groundChecker && _groundChecker.IsGrounded;
    public Vector2 Velocity => _rb ? _rb.linearVelocity : Vector2.zero;

    [Inject]
    public void Construct(WormConfigSO config, [InjectOptional] List<IHitStunState> hitStunStates = null)
    {
        _config = config;
        if (hitStunStates != null && hitStunStates.Count > 0)
            _hitStunStates = hitStunStates;
    }

    void Awake()
    {
        if (!_rb) _rb = GetComponent<Rigidbody2D>();
        if (!_facingTransform) _facingTransform = transform;
        if (!_ledgeGuard) _ledgeGuard = GetComponentInChildren<LedgeGuard2D>(true);
        if (!_groundChecker) _groundChecker = GetComponentInChildren<WormGroundChecker2D>(true);

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
    }

    public void Move(float speed, float acceleration, int dir)
    {
        if (_frozen || IsInHitStun() || !_rb) return;

        Face(dir);
        float targetX = dir * speed;
        float currentX = _rb.linearVelocity.x;
        float newX = Mathf.MoveTowards(currentX, targetX, acceleration * Time.fixedDeltaTime);
        _rb.linearVelocity = new Vector2(newX, _rb.linearVelocity.y);
    }

    public void ApplyDrag(float drag)
    {
        if (_rb) _rb.linearDamping = drag;
    }

    public void ResetDrag()
    {
        if (_rb) _rb.linearDamping = 0f;
    }

    public void SetVelocity(Vector2 v)
    {
        if (_rb && !_frozen) _rb.linearVelocity = v;
    }

    public void Face(int sign)
    {
        if (sign == 0) return;
        sign = sign >= 0 ? +1 : -1;
        if (_facingTransform)
        {
            var s = _facingTransform.localScale;
            s.x = _baseScaleX * sign;
            _facingTransform.localScale = s;
        }
        
        if (_ledgeGuard) _ledgeGuard.SetFacingSign(sign);
    }

    public void SetFrozen(bool frozen)
    {
        if (!_rb) return;
        if (_frozen == frozen) return;

        _frozen = frozen;
        if (frozen)
        {
            _savedConstraints = _rb.constraints;
            _rb.linearVelocity = Vector2.zero;
            _rb.constraints = RigidbodyConstraints2D.FreezeAll;
        }
        else
        {
            _rb.constraints = _savedConstraints;
        }
    }

    public bool IsLedgeAhead(int dir)
    {
        if (!_ledgeGuard) return false;
        // _config is a class field, should be accessible. Check if it's injected.
        LayerMask mask = _config != null ? _config.solidLayers : default;
        return _ledgeGuard.IsLedgeAhead(transform.position, dir, mask);
    }

    public bool IsWallAhead(int dir)
    {
        if (_config == null) return false;
        
        float checkDist = 0.2f;
        Vector2 origin = transform.position;
        // Simple raycast for wall check
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.right * dir, checkDist, _config.solidLayers);
        return hit.collider != null;
    }
    
    public bool CheckWallHit(out Vector2 normal)
    {
        normal = Vector2.zero;
        if (!_rb || _config == null) return false;

        ContactPoint2D[] contacts = new ContactPoint2D[10];
        int n = _rb.GetContacts(contacts);
        
        for(int i=0; i<n; i++)
        {
            // Simple check: horizontal normal opposing movement
            if (Mathf.Abs(contacts[i].normal.x) > 0.5f)
            {
                normal = contacts[i].normal;
                return true;
            }
        }
        return false;
    }

    public void NotifyBounceStarted()
    {
        if (_groundChecker) _groundChecker.NotifyBounceStarted();
    }

    bool IsInHitStun()
    {
        if (_hitStunStates == null) return false;
        for (int i = 0; i < _hitStunStates.Count; i++)
            if (_hitStunStates[i] != null && _hitStunStates[i].InHitStun) return true;
        return false;
    }
}
