using UnityEngine;

public class FloatMovement2D : MonoBehaviour
{
    [Header("Speeds")]
    [Min(0f)] public float patrolSpeed = 2f;
    [Min(0f)] public float fleeSpeed = 5f;

    [Header("Smoothing")]
    [Min(0f)] public float acceleration = 30f;
    [Min(0f)] public float deceleration = 30f;

    Rigidbody2D _rb;
    Vector2 _desiredVelocity;
    Vector2 _currentVelocity;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;
        _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    /// <summary>Задать желаемое движение (нормализуем внутри).</summary>
    public void MoveInDirection(Vector2 direction, float speed)
    {
        _desiredVelocity = direction.sqrMagnitude > 0.0001f
            ? direction.normalized * speed
            : Vector2.zero;
    }

    void FixedUpdate()
    {
        bool speedingUp = _desiredVelocity.magnitude > _currentVelocity.magnitude;
        float maxDelta = (speedingUp ? acceleration : deceleration) * Time.fixedDeltaTime;
        _currentVelocity = Vector2.MoveTowards(_currentVelocity, _desiredVelocity, maxDelta);

        _rb.linearVelocity = _currentVelocity;
    }
}
