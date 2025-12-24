using UnityEngine;
using UnityEngine.AI;
using Zenject;

public class MinionMotor2D : MonoBehaviour
{
    [SerializeField] NavMeshAgent _agent;
    [SerializeField] Transform _facingTransform;

    MinionConfig _config;
    bool _frozen;
    float _baseScaleX;
    float _baseScaleY;

    public bool IsMoving => _agent && !_agent.isStopped && _agent.hasPath;

    public Vector2 Velocity => _agent ? (Vector2)_agent.velocity : Vector2.zero;

    [Inject]
    public void Construct(MinionConfig config)
    {
        _config = config;
    }

    void Awake()
    {
        if (!_agent) _agent = GetComponent<NavMeshAgent>();
        if (!_facingTransform) _facingTransform = transform;

        _baseScaleX = Mathf.Abs(_facingTransform.localScale.x);
        _baseScaleY = Mathf.Abs(_facingTransform.localScale.y);
        if (_baseScaleX < 0.0001f) _baseScaleX = 1f;
        if (_baseScaleY < 0.0001f) _baseScaleY = 1f;

        if (_agent)
        {
            _agent.updateRotation = false;
            _agent.updateUpAxis = false;
        }
    }

    void Update()
    {
        if (_agent && _config)
        {
            _agent.acceleration = _config.acceleration;
            _agent.angularSpeed = _config.angularSpeed;
        }

        if (_agent && _agent.velocity.sqrMagnitude > 0.1f)
        {
            RotateTowards(_agent.velocity);
        }
    }

    public void MoveTo(Vector2 position, float speed)
    {
        if (!_agent || _frozen) return;
        
        _agent.isStopped = false;
        _agent.speed = speed;
        _agent.SetDestination(position);
    }

    public void Stop()
    {
        if (!_agent) return;
        _agent.isStopped = true;
        _agent.velocity = Vector3.zero;
    }

    public void SetFrozen(bool frozen)
    {
        _frozen = frozen;
        if (frozen) Stop();
    }

    public void RotateTowards(Vector2 dir)
    {
        if (!_facingTransform) return;
        
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        _facingTransform.rotation = Quaternion.Euler(0, 0, angle);

        // Prevent "upside down" look by flipping Y scale if facing left-ish
        Vector3 s = _facingTransform.localScale;
        
        // Always reset X to positive base since we use rotation now
        s.x = _baseScaleX;
        
        if (Mathf.Abs(angle) > 90f)
        {
            s.y = -_baseScaleY;
        }
        else
        {
            s.y = _baseScaleY;
        }
        
        _facingTransform.localScale = s;
    }

    public void FaceTowards(Vector2 target)
    {
        RotateTowards(target - (Vector2)transform.position);
    }

    public void Warp(Vector2 position)
    {
        if (_agent) _agent.Warp(position);
        else transform.position = position;
    }
}
