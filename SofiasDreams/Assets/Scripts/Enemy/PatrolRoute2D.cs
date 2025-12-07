using UnityEngine;

public class PatrolRoute2D : MonoBehaviour
{
    [Header("Identity")]
    [Tooltip("Ключ группы. Враг с таким же routeKey автоматически привяжется к этому маршруту.")]
    public string routeKey;

    [Header("Source (optional)")]
    [Tooltip("Если указать — все дочерние трансформы станут точками (их мировые позиции).")]
    public Transform anchorsParent;

    [Header("Route (world-space)")]
    public Vector3[] worldPoints = new Vector3[0];

    [Min(0f)] public float arriveDistance = 0.2f;

    [Header("Gizmos")]
    public bool showGizmos = true;
    public Color pathColor = new Color(0f, 1f, 1f, 0.9f);
    public float pointRadius = 0.1f;

    public int Count => worldPoints != null ? worldPoints.Length : 0;

    void Awake()  { TryBakeFromChildren(); }
    void OnValidate() { TryBakeFromChildren(); }

    public void TryBakeFromChildren()
    {
        if (!anchorsParent) return;
        int n = anchorsParent.childCount;
        if (n <= 0) return;

        worldPoints = new Vector3[n];
        for (int i = 0; i < n; i++)
            worldPoints[i] = anchorsParent.GetChild(i).position;
    }

    public Vector3 GetWaypointWorld(int index)
    {
        if (Count == 0) return transform.position;
        index = Mathf.Clamp(index, 0, Count - 1);
        return worldPoints[index];
    }

    public int NextIndexLoop(int index) => Count == 0 ? 0 : (index + 1) % Count;

    public Vector3 GetCentroid()
    {
        if (Count == 0) return transform.position;
        Vector3 sum = Vector3.zero;
        for (int i = 0; i < Count; i++) sum += worldPoints[i];
        return sum / Count;
    }

    void OnDrawGizmos()
    {
        if (!showGizmos || Count == 0) return;

        Gizmos.color = pathColor;
        Vector3 first = worldPoints[0];
        Vector3 prev  = first;
        for (int i = 0; i < Count; i++)
        {
            Vector3 p = worldPoints[i];
            Gizmos.DrawSphere(p, pointRadius);
            Gizmos.DrawLine(prev, p);
            prev = p;
        }
        Gizmos.DrawLine(prev, first); // замкнуто
    }
}
