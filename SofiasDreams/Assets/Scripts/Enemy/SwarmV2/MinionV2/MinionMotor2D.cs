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
        if (_baseScaleX < 0.0001f) _baseScaleX = 1f;

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
            Face(_agent.velocity.x >= 0 ? 1 : -1);
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

    public void Face(int sign)
    {
        if (!_facingTransform) return;
        sign = sign >= 0 ? 1 : -1;
        var s = _facingTransform.localScale;
        s.x = _baseScaleX * sign;
        _facingTransform.localScale = s;
    }

    public void FaceTowards(Vector2 target)
    {
        float dx = target.x - transform.position.x;
        if (Mathf.Abs(dx) > 0.1f)
            Face(dx > 0 ? 1 : -1);
    }

    public void Warp(Vector2 position)
    {
        if (_agent) _agent.Warp(position);
        else transform.position = position;
    }
}
