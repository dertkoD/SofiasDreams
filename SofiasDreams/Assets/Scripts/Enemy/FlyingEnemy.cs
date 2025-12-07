using UnityEngine;

public class FlyingEnemy : MonoBehaviour
{
    [SerializeField] private float speed = 2f;
    [SerializeField] private float width = 2f;
    [SerializeField] private float height = 1f;

    private Vector3 startPosition;
    private float time = 0f;

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        time += Time.deltaTime * speed;

        float x = width * Mathf.Sin(time);
        float y = height * Mathf.Sin(time) * Mathf.Cos(time);

        transform.position = startPosition + new Vector3(x, y, 0);

        transform.localScale = new Vector3(Mathf.Sign(Mathf.Cos(time)), 1, 1);
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector3 currentPosition = transform.position;
        Vector3 prevPoint = currentPosition;

        for (float t = 0; t < Mathf.PI * 2; t += 0.1f)
        {
            float x = width * Mathf.Sin(t);
            float y = height * Mathf.Sin(t) * Mathf.Cos(t);
            Vector3 nextPoint = currentPosition + new Vector3(x, y, 0);
            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }
        Gizmos.DrawLine(prevPoint, currentPosition);

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(currentPosition, 0.1f);
    }
}

