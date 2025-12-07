using UnityEngine;

public class FlyingEnemyAnotherType : MonoBehaviour
{
    [SerializeField] private Transform[] pathPoints; // Точки пути в инспекторе
    [SerializeField] private float moveSpeed = 2.0f; // Скорость движения, регулируемая в инспекторе

    private int currentPointIndex = 0; // Индекс текущей точки
    private Transform targetPoint; // Текущая целевая точка

    void Start()
    {
        if (pathPoints == null || pathPoints.Length < 2)
        {
            Debug.LogWarning("Need at least 2 path points for enemy movement!");
            return;
        }

        // Устанавливаем первую точку как начальную
        currentPointIndex = 0;
        targetPoint = pathPoints[currentPointIndex];
    }

    void Update()
    {
        if (targetPoint == null) return;

        // Движение к целевой точке
        transform.position = Vector3.MoveTowards(transform.position, targetPoint.position, moveSpeed * Time.deltaTime);

        // Проверка достижения точки
        if (Vector3.Distance(transform.position, targetPoint.position) < 0.1f)
        {
            // Переход к следующей точке
            currentPointIndex = (currentPointIndex + 1) % pathPoints.Length;
            targetPoint = pathPoints[currentPointIndex];
        }
    }

    void OnDrawGizmos()
    {
        if (pathPoints == null || pathPoints.Length < 2) return;

        // Рисуем линию пути
        Gizmos.color = Color.yellow;
        for (int i = 0; i < pathPoints.Length - 1; i++)
        {
            if (pathPoints[i] != null && pathPoints[i + 1] != null)
            {
                Gizmos.DrawLine(pathPoints[i].position, pathPoints[i + 1].position);
            }
        }
        // Замыкаем путь к первой точке
        if (pathPoints.Length > 1 && pathPoints[0] != null && pathPoints[pathPoints.Length - 1] != null)
        {
            Gizmos.DrawLine(pathPoints[pathPoints.Length - 1].position, pathPoints[0].position);
        }

        // Рисуем текущую позицию врага (опционально)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.2f);
    }
}
