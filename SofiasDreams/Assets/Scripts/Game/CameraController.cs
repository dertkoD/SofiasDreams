using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private float smoothTime = 0.3f;
    [SerializeField] private List<List<Transform>> pathGroups = new List<List<Transform>>(); // Список групп путей
    [SerializeField] private float followZoneRadius = 2.0f; // Радиус зоны свободы игрока

    private Vector3 velocity = Vector3.zero;
    private Camera cam;
    private int currentPathGroupIndex = 0; // Индекс текущей группы путей
    private int currentPathIndex = 0; // Индекс текущего сегмента в группе

    void Start()
    {
        cam = GetComponent<Camera>();

        // Проверка и инициализация
        if (pathGroups == null || pathGroups.Count == 0 || (pathGroups.Count > 0 && pathGroups[0] == null) || (pathGroups.Count > 0 && pathGroups[0].Count < 2))
        {
            Debug.LogWarning("Need at least one path group with 2 or more points for camera movement! Initializing empty groups.");
            pathGroups = new List<List<Transform>> { new List<Transform>() }; // Минимальная инициализация
        }

        // Инициализация позиции камеры на первой точке первого пути
        if (pathGroups[0].Count > 0)
        {
            transform.position = new Vector3(pathGroups[0][0].position.x, pathGroups[0][0].position.y, transform.position.z);
        }
        SelectPathGroup(); // Выбираем начальную группу на основе игрока
    }

    void Update()
    {
        if (player == null || pathGroups == null || pathGroups.Count == 0 || pathGroups[currentPathGroupIndex] == null) return;

        // Проверяем, находится ли игрок в зоне свободы
        Vector3 playerToCamera = transform.position - player.position;
        if (playerToCamera.magnitude > followZoneRadius)
        {
            SelectPathGroup(); // Переключаем путь, если игрок далеко
            Vector3 targetPosition = GetTargetPositionOnPath();
            targetPosition.z = transform.position.z;
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
        }
    }

    private void SelectPathGroup()
    {
        if (pathGroups == null || pathGroups.Count == 0) return;

        float minDistance = float.MaxValue;
        int newPathGroupIndex = currentPathGroupIndex;

        for (int i = 0; i < pathGroups.Count; i++)
        {
            if (pathGroups[i] == null || pathGroups[i].Count < 2) continue;

            // Находим ближайшую точку в группе путей
            float groupDistance = float.MaxValue;
            for (int j = 0; j < pathGroups[i].Count; j++)
            {
                float distance = Vector3.Distance(player.position, pathGroups[i][j].position);
                if (distance < groupDistance)
                {
                    groupDistance = distance;
                }
            }

            if (groupDistance < minDistance)
            {
                minDistance = groupDistance;
                newPathGroupIndex = i;
            }
        }

        if (newPathGroupIndex != currentPathGroupIndex)
        {
            currentPathGroupIndex = newPathGroupIndex;
            currentPathIndex = GetNearestPathSegment(); // Сбрасываем индекс на ближайший сегмент
            Debug.Log("Switched to path group: " + currentPathGroupIndex);
        }
    }

    private int GetNearestPathSegment()
    {
        List<Transform> currentPath = pathGroups[currentPathGroupIndex];
        float minDistance = float.MaxValue;
        int nearestIndex = currentPathIndex;

        for (int i = 0; i < currentPath.Count - 1; i++)
        {
            Vector3 startPoint = currentPath[i].position;
            Vector3 endPoint = currentPath[i + 1].position;
            float distance = PointToSegmentDistance(player.position, startPoint, endPoint);

            if (distance < minDistance)
            {
                minDistance = distance;
                nearestIndex = i;
            }
        }

        return nearestIndex;
    }

    private float PointToSegmentDistance(Vector3 point, Vector3 start, Vector3 end)
    {
        Vector3 segment = end - start;
        Vector3 pointVector = point - start;
        float segmentLengthSquared = segment.sqrMagnitude;
        if (segmentLengthSquared == 0) return pointVector.magnitude;

        float t = Mathf.Clamp01(Vector3.Dot(pointVector, segment) / segmentLengthSquared);
        Vector3 projection = start + t * segment;
        return Vector3.Distance(point, projection);
    }

    private Vector3 GetTargetPositionOnPath()
    {
        List<Transform> currentPath = pathGroups[currentPathGroupIndex];
        int nextIndex = (currentPathIndex + 1) % currentPath.Count;
        Vector3 startPoint = currentPath[currentPathIndex].position;
        Vector3 endPoint = currentPath[nextIndex].position;

        // Интерполяция вдоль сегмента пути с учетом позиции игрока
        Vector3 direction = (endPoint - startPoint).normalized;
        float segmentLength = Vector3.Distance(startPoint, endPoint);
        float distanceFromStart = Mathf.Clamp(Vector3.Dot(player.position - startPoint, direction), 0, segmentLength);
        return startPoint + direction * distanceFromStart;
    }

    void OnDrawGizmos()
    {
        if (pathGroups == null || pathGroups.Count == 0) return;

        // Рисуем линии для всех групп путей
        for (int g = 0; g < pathGroups.Count; g++)
        {
            if (pathGroups[g] == null || pathGroups[g].Count < 2) continue;

            Gizmos.color = Color.green; // Разные цвета для разных путей (можно расширить)
            for (int i = 0; i < pathGroups[g].Count - 1; i++)
            {
                if (pathGroups[g][i] != null && pathGroups[g][i + 1] != null)
                {
                    Gizmos.DrawLine(pathGroups[g][i].position, pathGroups[g][i + 1].position);
                }
            }
            // Замыкаем путь в каждой группе
            if (pathGroups[g].Count > 1 && pathGroups[g][0] != null && pathGroups[g][pathGroups[g].Count - 1] != null)
            {
                Gizmos.DrawLine(pathGroups[g][pathGroups[g].Count - 1].position, pathGroups[g][0].position);
            }
        }

        // Рисуем зону свободы игрока
        if (player != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(player.position, followZoneRadius);
        }
    }
}
