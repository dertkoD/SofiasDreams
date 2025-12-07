using UnityEngine;

public class VisionCone2D : MonoBehaviour
{
    [Header("Search")]
    public LayerMask targetLayers;
    [Min(0f)] public float radius = 6f;
    [Range(0f, 360f)] public float fov = 360f;

    [Header("Line of Sight")]
    public bool requireLineOfSight = false;
    public LayerMask obstacleLayers;

    [Header("Facing")]
    public bool faceByScaleX = true;
    public int fixedFacingSign = 1;

    [Header("Debug")]
    public bool showGizmos = true;
    public Color gizmoColor = new Color(1, 1, 0, 0.35f);

    const float kEps = 1e-5f;
    const float kEdgeTolDeg = 0.5f; // допуск на границе сектора

    public bool TryGetClosestTarget(out Transform target)
    {
        target = null;
        if (targetLayers.value == 0) return false;

        var hits = Physics2D.OverlapCircleAll(transform.position, radius, targetLayers);
        if (hits == null || hits.Length == 0) return false;

        float best = float.PositiveInfinity;
        Transform bestT = null;

        Vector2 origin = transform.position;          // ← ОДИН РАЗ как Vector2

        foreach (var h in hits)
        {
            if (!h) continue;
            if (h.transform == transform || h.transform.root == transform.root) continue;

            if (!IsInFOV(h)) continue;                // ← передаём Collider2D
            if (requireLineOfSight && !HasLineOfSight(h)) continue;

            Vector2 p = (Vector2)h.bounds.ClosestPoint(origin); // ← к Vector2
            float d = (p - origin).sqrMagnitude;                // ← Vector2−Vector2

            if (d < best) { best = d; bestT = h.transform; }
        }

        target = bestT;
        return target != null;
    }

    bool IsInFOV(Collider2D col)
    {
        Vector2 origin = transform.position;

        // Ближайшая точка коллайдера к конусу. Это даёт срабатывание даже «чуть-чуть» краем.
        Vector2 p = col.bounds.ClosestPoint(origin);
        Vector2 to = p - origin;

        // Проверка радиуса по ближайшей точке, а не по центру
        if (to.sqrMagnitude > radius * radius + kEps) return false;
        if (fov >= 360f) return true;

        // Если почти в точке — считаем, что в секторе
        Vector2 fwd = GetForward();
        if (to.sqrMagnitude < kEps) return true;

        float ang = Vector2.Angle(fwd, to);
        return ang <= (fov * 0.5f + kEdgeTolDeg);
    }

    bool HasLineOfSight(Collider2D targetCol)
    {
        if (!requireLineOfSight) return true;

        Vector2 origin = transform.position;
        Vector2 dest = targetCol.bounds.ClosestPoint(origin);
        Vector2 dir = dest - origin;
        float dist = dir.magnitude;
        if (dist <= kEps) return true;

        var hit = Physics2D.Raycast(origin, dir.normalized, dist, obstacleLayers);
        return hit.collider == null;
    }

    Vector2 GetForward()
    {
        if (faceByScaleX)
        {
            float sign = Mathf.Sign(transform.lossyScale.x);
            if (Mathf.Approximately(sign, 0f)) sign = 1f;
            return new Vector2(sign, 0f);
        }
        float s = Mathf.Sign(fixedFacingSign);
        return new Vector2(s == 0 ? 1f : s, 0f);
    }

    void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        Gizmos.color = gizmoColor;
        Vector3 o = transform.position;
        Gizmos.DrawWireSphere(o, radius);

        if (fov < 360f)
        {
            Vector2 fwd = GetForward();
            float half = fov * 0.5f;
            Vector3 left  = Quaternion.Euler(0, 0,  half) * (Vector3)fwd;
            Vector3 right = Quaternion.Euler(0, 0, -half) * (Vector3)fwd;
            Gizmos.DrawLine(o, o + left  * radius);
            Gizmos.DrawLine(o, o + right * radius);
        }
        else
        {
            Vector2 fwd = GetForward();
            Gizmos.DrawLine(o, o + (Vector3)fwd * radius * 0.6f);
        }
    }
}
