using UnityEngine;

public class EnemySpawnPoint : MonoBehaviour
{
    [Header("Patrol")]
    [SerializeField] EnemyPatrolPath _patrolPath;

    [Header("Gizmos")]
    [SerializeField] Color _gizmoColor = Color.red;
    [SerializeField] float _radius = 0.25f;

    public Vector3 Position => transform.position;
    public EnemyPatrolPath PatrolPath => _patrolPath;

    void Reset()
    {
        if (_patrolPath == null)
            _patrolPath = GetComponentInChildren<EnemyPatrolPath>();
    }

    #if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = _gizmoColor;

        // сам спавн
        Gizmos.DrawWireSphere(transform.position, _radius);
        Gizmos.DrawLine(transform.position + Vector3.left * _radius * 0.5f,
            transform.position + Vector3.right * _radius * 0.5f);
        Gizmos.DrawLine(transform.position + Vector3.up * _radius * 0.5f,
            transform.position + Vector3.down * _radius * 0.5f);

        // линия к первой точке патруля, если есть путь
        if (_patrolPath != null && _patrolPath.Count > 0)
        {
            Vector3 firstPoint = _patrolPath.GetPoint(0);
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, firstPoint);
        }
    }
    #endif
}
