using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    public Vector2 pointA;
    public Vector2 pointB;
    public float speed = 2f;

    private Rigidbody2D rb;
    private Vector2 target;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        target = pointB;
    }

    void FixedUpdate()
    {
        Vector2 newPosition = Vector2.MoveTowards(rb.position, target, speed * Time.fixedDeltaTime);
        rb.MovePosition(newPosition);

        if (Vector2.Distance(rb.position, target) < 0.05f)
        {
            target = target == pointA ? pointB : pointA;
        }
    }

    void OnDrawGizmos()
    {
        if (Camera.current != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(pointA, 0.5f);
            Gizmos.DrawWireSphere(pointB, 0.5f);
        }
    }
}
