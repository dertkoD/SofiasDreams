using UnityEngine;

public class EnemySpawnPoint : MonoBehaviour
{
    [Header("Patrol")]
    [SerializeField] EnemyMovementMode _kind = EnemyMovementMode.GroundOnly;
    [SerializeField] public EnemyPatrolPath _patrolPath;

    [Header("Gizmos")]
    [SerializeField] Color _gizmoColorGroundEnemy = Color.red;
    [SerializeField] Color _gizmoColorFlyingEnemy = Color.magenta;
    [SerializeField] Color _gizmoColorJumpingEnemy = Color.yellow;
    [SerializeField] Color _gizmoColorWormEnemy = new Color(1.0f, 0.5f, 0.0f); // Orange
    [SerializeField] float _radius = 0.25f;

    public Vector3 Position => transform.position;
    public EnemyMovementMode Kind      => _kind;
    public EnemyPatrolPath PatrolPath => _patrolPath;

    void Reset()
    {
        if (_patrolPath == null)
            _patrolPath = GetComponentInChildren<EnemyPatrolPath>();
    }

    #if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Color c = _gizmoColorGroundEnemy;
        switch (_kind)
        {
            case EnemyMovementMode.Planar2D: c = _gizmoColorFlyingEnemy; break;
            case EnemyMovementMode.Jumping:  c = _gizmoColorJumpingEnemy; break;
            case EnemyMovementMode.Worm:     c = _gizmoColorWormEnemy; break;
        }
        Gizmos.color = c;

        Gizmos.DrawWireSphere(transform.position, _radius);
        Gizmos.DrawLine(transform.position + Vector3.left * _radius * 0.5f,
            transform.position + Vector3.right * _radius * 0.5f);
        Gizmos.DrawLine(transform.position + Vector3.up * _radius * 0.5f,
            transform.position + Vector3.down * _radius * 0.5f);

        if (_patrolPath != null && _patrolPath.Count > 0)
        {
            Vector3 firstPoint = _patrolPath.GetPoint(0);
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, firstPoint);
        }
    }
    #endif
}
