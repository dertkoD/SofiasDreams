using System.Linq;
using UnityEngine;

[DefaultExecutionOrder(-10)]
public class EnemyPatrolBinder2D : MonoBehaviour
{
    [Header("Binding")]
    [Tooltip("Явно указанный маршрут (если задан — используется он).")]
    public PatrolRoute2D overrideRoute;

    [Tooltip("Ключ маршрута. Если задан, ищется PatrolRoute2D с таким же routeKey.")]
    public string routeKey;

    [Min(0f)] public float searchRadius = 100f;
    public bool pickNearestIfKeyMissing = true;

    [Header("Fallback Route (auto)")]
    public bool createFallbackIfNone = true;
    [Min(0.1f)] public float fallbackRadius = 3f;
    [Range(3, 64)] public int fallbackPoints = 6;

    [Header("Fallback Gizmos")]
    public bool showFallbackGizmos = true;
    public bool showEvenWhenBound = true;       // рисовать, даже если маршрут найден
    public bool drawWhenNotSelected = false;    // рисовать всегда, а не только в Selected
    [Range(8, 128)] public int gizmoCircleSegments = 48;
    public Color fallbackAreaColor = new Color(0f, 1f, 0f, 0.06f);
    public Color fallbackLineColor = new Color(0f, 1f, 0f, 0.9f);
    public float fallbackPointRadius = 0.08f;

    PatrolRoute2D _boundRoute;
    public PatrolRoute2D BoundRoute => _boundRoute;

    Vector3[] _fallbackPreview; // точки круга для гизмоса

    void OnValidate()
    {
        ClampPreview();
        UpdateFallbackPreview();
    }

    void Awake()
    {
        BindNow();
        UpdateFallbackPreview();
    }

    public void BindNow()
    {
        // 1) Явный маршрут
        if (overrideRoute)
        {
            _boundRoute = overrideRoute;
            return;
        }

        // 2) По ключу (или ближайший)
        var all = FindObjectsOfType<PatrolRoute2D>(true);
        Vector3 pos = transform.position;

        var candidates = string.IsNullOrEmpty(routeKey)
            ? all
            : all.Where(r => r && r.routeKey == routeKey).ToArray();

        if (candidates.Length == 0 && pickNearestIfKeyMissing)
            candidates = all;

        _boundRoute = null;
        if (candidates.Length > 0)
        {
            float best = float.PositiveInfinity;
            foreach (var r in candidates)
            {
                if (!r || r.Count == 0) continue;
                float d = Vector3.Distance(pos, r.GetCentroid());
                if (d <= searchRadius && d < best) { best = d; _boundRoute = r; }
            }
        }

        // 3) Фоллбек: локальный круг
        if (_boundRoute == null && createFallbackIfNone)
        {
            var route = gameObject.AddComponent<PatrolRoute2D>();
            route.worldPoints = GenerateCircle(transform.position, fallbackRadius, fallbackPoints);
            route.arriveDistance = 0.2f;
            _boundRoute = route;
        }
    }

    // ---------- Gizmos ----------
    void OnDrawGizmos()
    {
        if (drawWhenNotSelected)
            DrawFallbackGizmos();
    }

    void OnDrawGizmosSelected()
    {
        if (!drawWhenNotSelected)
            DrawFallbackGizmos();
    }

    void DrawFallbackGizmos()
    {
        if (!showFallbackGizmos) return;
        if (_boundRoute != null && !showEvenWhenBound) return;

        // Кольцо радиуса
        if (_fallbackPreview == null || _fallbackPreview.Length == 0)
            UpdateFallbackPreview();

        // Заливка (условно) — просто окружность + точки
        // Линии окружности
        Gizmos.color = fallbackLineColor;
        for (int i = 0; i < _fallbackPreview.Length; i++)
        {
            var a = _fallbackPreview[i];
            var b = _fallbackPreview[(i + 1) % _fallbackPreview.Length];
            Gizmos.DrawLine(a, b);
        }

        // Точки
        for (int i = 0; i < _fallbackPreview.Length; i++)
            Gizmos.DrawSphere(_fallbackPreview[i], fallbackPointRadius);

        // Слабая «заливка» кругом (имитация — рисуем чуть меньший круг пунктиром)
        Gizmos.color = fallbackAreaColor;
        var inner = GenerateCircle(transform.position, Mathf.Max(0.001f, fallbackRadius * 0.98f), gizmoCircleSegments);
        for (int i = 0; i < inner.Length; i++)
        {
            var a = inner[i];
            var b = inner[(i + 1) % inner.Length];
            Gizmos.DrawLine(a, b);
        }
    }

    void UpdateFallbackPreview()
    {
        _fallbackPreview = GenerateCircle(transform.position, fallbackRadius, gizmoCircleSegments);
    }

    void ClampPreview()
    {
        if (fallbackPoints < 3) fallbackPoints = 3;
        if (gizmoCircleSegments < 8) gizmoCircleSegments = 8;
    }

    static Vector3[] GenerateCircle(Vector3 center, float radius, int count)
    {
        var pts = new Vector3[count];
        float step = Mathf.PI * 2f / count;
        for (int i = 0; i < count; i++)
        {
            float a = step * i;
            pts[i] = center + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * radius;
        }
        return pts;
    }
}
