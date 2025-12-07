using UnityEngine;

public class OrbitPatrol2D : MonoBehaviour
{
    [Min(0.1f)] public float radius = 3f;
    [Min(0f)]   public float tangentialSpeed = 3f;
    public bool clockwise = true;

    [Header("Radius keeping (spring)")]
    [Min(0f)] public float radialStiffness = 4f;  // чем больше — тем жёстче держит радиус
    [Min(0f)] public float radialDamping   = 1f;  // «вязкость» по радиусу

    [Header("Gizmos")]
    public bool showGizmos = true;
    public Color circleColor = new Color(0f, 0.7f, 1f, 0.8f);

    Vector2 _lastRadialVel;

    public Vector2 GetDesiredVelocity(Vector2 selfPos, Vector2 selfVel, Vector2 targetPos)
    {
        Vector2 r = selfPos - targetPos;
        float dist = r.magnitude;
        if (dist < 1e-4f) r = Vector2.right; // защита от деления на 0

        Vector2 radialDir = r / Mathf.Max(dist, 1e-4f);

        // Касательная (перпендикуляр к радиусу)
        Vector2 tangent = clockwise ? new Vector2(radialDir.y, -radialDir.x)
                                    : new Vector2(-radialDir.y, radialDir.x);

        // Блок для удержания радиуса (пружина + демпфер по радиальному направлению)
        float radialError = dist - radius;                         // >0 — далеко; <0 — близко
        float springAccel = -radialError * radialStiffness;        // тянем к радиусу
        float radialVel    = Vector2.Dot(selfVel, radialDir);
        float dampingAccel = -radialVel * radialDamping;

        float radialControl = springAccel + dampingAccel;
        Vector2 radialVelDelta = radialDir * radialControl * Time.fixedDeltaTime;
        _lastRadialVel = radialVelDelta; // можно выводить в дебаг при желании

        // Итоговая желаемая скорость: вдоль касательной + корректирующая радиальная
        Vector2 v = tangent * tangentialSpeed + radialVelDelta / Time.fixedDeltaTime;
        return v;
    }

    void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;
        Gizmos.color = circleColor;
        const int N = 64;
        float step = Mathf.PI * 2f / N;
        Vector3 center = transform.position;
        Vector3 prev = center + new Vector3(radius, 0f, 0f);
        for (int i = 1; i <= N; i++)
        {
            float a = step * i;
            Vector3 p = center + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * radius;
            Gizmos.DrawLine(prev, p);
            prev = p;
        }
    }
}
