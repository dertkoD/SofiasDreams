using UnityEngine;
using Zenject;

public class LazyMiniBossMotor2D : MonoBehaviour
{
    [SerializeField] Rigidbody2D _rb;
    [SerializeField] Transform _facingTransform;

    LazyMiniBossConfigSO _config;
    bool _frozen;
    RigidbodyConstraints2D _savedConstraints;
    float _baseScaleX;

    [Inject]
    public void Construct(LazyMiniBossConfigSO config)
    {
        _config = config;
    }

    void Awake()
    {
        if (!_rb) _rb = GetComponent<Rigidbody2D>();
        if (!_facingTransform) _facingTransform = transform;
        if (_facingTransform) _baseScaleX = Mathf.Abs(_facingTransform.localScale.x);
        if (_baseScaleX < 0.001f) _baseScaleX = 1f;
        if (_rb) _savedConstraints = _rb.constraints;
    }

    public void Move(float velocityX)
    {
        if (_frozen || !_rb) return;
        
        Vector2 vel = _rb.linearVelocity;
        vel.x = velocityX;
        _rb.linearVelocity = vel;

        if (Mathf.Abs(velocityX) > 0.01f)
        {
            Face(velocityX >= 0 ? 1 : -1);
        }
    }

    public void Stop()
    {
        if (_rb)
        {
            Vector2 vel = _rb.linearVelocity;
            vel.x = 0;
            _rb.linearVelocity = vel;
        }
    }

    public void Face(int sign)
    {
        if (!_facingTransform) return;
        Vector3 s = _facingTransform.localScale;
        s.x = _baseScaleX * (sign >= 0 ? 1 : -1);
        _facingTransform.localScale = s;
    }

    public void SetFrozen(bool frozen)
    {
        if (_frozen == frozen) return;
        _frozen = frozen;

        if (_rb)
        {
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
    }

    public bool IsFacingRight => _facingTransform.localScale.x > 0;
    public Vector2 Velocity => _rb ? _rb.linearVelocity : Vector2.zero;
}
