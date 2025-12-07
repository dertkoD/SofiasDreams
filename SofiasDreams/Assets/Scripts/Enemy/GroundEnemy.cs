using UnityEngine;

public class GroundEnemy : MonoBehaviour
{
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;
    [SerializeField] private float speed = 2f;

    private Vector3 targetPoint;
    private bool movingToPointA = true;

    void Start()
    {
        targetPoint = pointA.position;
    }

    void Update()
    {
        Vector3 target = new Vector3(targetPoint.x, transform.position.y, 0);
        transform.position = Vector2.MoveTowards(transform.position, target, speed * Time.deltaTime);

        if (Vector2.Distance(transform.position, target) < 0.1f)
        {
            movingToPointA = !movingToPointA;
            targetPoint = movingToPointA ? pointA.position : pointB.position;
            transform.localScale = new Vector3(movingToPointA ? -1 : 1, 1, 1);
        }
    }
}
