using UnityEngine;

public class ObstacleSlide2D : MonoBehaviour
{
    [Header("Refs")]
    public GuardTargetBinder2D binder;   // центр орбиты (рой)
    public OrbitPatrol2D orbit;          // даёт tangentialSpeed + clockwise

    [Header("Collisions")]
    public LayerMask obstacleLayers;     // слои стен/пола/потолка (НЕ триггеры)
    [Min(0.05f)] public float minFlipInterval = 0.15f; // защита от дребезга
    [Min(0f)]    public float minSpeedForFlip = 0.1f;  // не разворачивать «из статики»
    [Min(0f)]    public float nudgeImpulse = 0.4f;     // микро-пинок от стены, чтобы не залипать

    Rigidbody2D _rb;
    float _lastFlipAt = -999f;

    void Reset()
    {
        binder = GetComponent<GuardTargetBinder2D>();
        orbit  = GetComponent<OrbitPatrol2D>();
    }

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        if (!binder) binder = GetComponent<GuardTargetBinder2D>();
        if (!orbit)  orbit  = GetComponent<OrbitPatrol2D>();
    }

    void OnCollisionEnter2D(Collision2D c)
    {
        if (((1 << c.collider.gameObject.layer) & obstacleLayers.value) == 0) return;
        if (Time.time - _lastFlipAt < minFlipInterval) return;
        if (_rb.linearVelocity.sqrMagnitude < minSpeedForFlip * minSpeedForFlip) return;

        // 1) Поменять направление орбиты
        orbit.clockwise = !orbit.clockwise;
        _lastFlipAt = Time.time;

        // 2) Сразу задать касательную скорость в НОВУЮ сторону
        Vector2 center = binder && binder.Target ? (Vector2)binder.Target.position : _rb.position;
        Vector2 radial = _rb.position - center;
        if (radial.sqrMagnitude < 1e-6f) radial = Vector2.right;
        radial.Normalize();

        // касательные: CW = ( +y, -x ), CCW = ( -y, +x )
        Vector2 tangentCW  = new Vector2(radial.y, -radial.x);
        Vector2 tangentCCW = -tangentCW;

        Vector2 tangent = orbit.clockwise ? tangentCW : tangentCCW;
        float   speed   = Mathf.Max(orbit.tangentialSpeed, _rb.linearVelocity.magnitude);

        _rb.linearVelocity = tangent * speed;

        // 3) Лёгкий «отскок» от контакта, чтобы сразу выйти из коллизии
        var contact = c.GetContact(0);
        _rb.AddForce(contact.normal * nudgeImpulse, ForceMode2D.Impulse);
    }
}
