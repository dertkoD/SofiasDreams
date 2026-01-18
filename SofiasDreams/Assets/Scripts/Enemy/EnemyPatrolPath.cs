using UnityEngine;

public class EnemyPatrolPath : MonoBehaviour
{
    [SerializeField] Transform[] _points;

    public int Count => _points != null ? _points.Length : 0;

    public Vector3 GetPoint(int index)
    {
        if (_points == null || _points.Length == 0)
            return transform.position;

        index = Mathf.Clamp(index, 0, _points.Length - 1);
        return _points[index] ? _points[index].position : transform.position;
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (_points == null || _points.Length == 0)
            return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < _points.Length; i++)
        {
            if (!_points[i]) continue;

            Gizmos.DrawSphere(_points[i].position, 0.08f);

            if (i + 1 < _points.Length && _points[i + 1])
                Gizmos.DrawLine(_points[i].position, _points[i + 1].position);
        }
    }
#endif
}
