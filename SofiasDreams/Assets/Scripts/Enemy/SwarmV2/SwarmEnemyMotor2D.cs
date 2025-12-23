using UnityEngine;
using UnityEngine.AI;
using Zenject;

public class SwarmEnemyMotor2D : MonoBehaviour
{
    [SerializeField] NavMeshAgent _agent;
    [SerializeField] Transform _facingTransform;

    SwarmConfig _config;
    bool _frozen;
    float _baseScaleX;

    public Vector2 Velocity => _agent ? (Vector2)_agent.velocity : Vector2.zero;

    [Inject]
    public void Construct(SwarmConfig config)
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
            // Speed is set by MoveTo
        }

        // Face direction of movement
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
        // User requested NOT to flip the main transform scale.
        // We only flip the _facingTransform IF it is NOT the root transform,
        // OR we just don't flip at all if that's what is asked.
        
        // Assuming the visual sprite is a child object assigned to _facingTransform.
        // If _facingTransform == transform, then we are flipping the whole object which is bad for children (minions).
        
        if (!_facingTransform) return;
        
        // Safety check: if facing transform IS the root, do nothing to avoid flipping children.
        if (_facingTransform == transform) return;

        sign = sign >= 0 ? 1 : -1;
        var s = _facingTransform.localScale;
        s.x = _baseScaleX * sign;
        _facingTransform.localScale = s;
    }

    public void Warp(Vector2 position)
    {
        if (_agent) _agent.Warp(position);
        else transform.position = position;
    }
}
