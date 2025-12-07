using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BreakingPlatform : MonoBehaviour
{
    public Tilemap tilemap;                       // Ссылка на tilemap с платформой
    public float delayBetweenBreaks = 0.2f;       // Задержка между разрушениями
    public Vector2 detectionSize = new Vector2(1f, 0.2f); // Размер области для обнаружения игрока
    public LayerMask playerLayer;

    private bool alreadyTriggered = false;

    void Update()
    {
        if (!alreadyTriggered)
        {
            Collider2D hit = Physics2D.OverlapBox(transform.position, detectionSize, 0f, playerLayer);
            if (hit != null)
            {
                alreadyTriggered = true;
                StartCoroutine(BreakTilesLeftToRight());
            }
        }
    }

    IEnumerator BreakTilesLeftToRight()
    {
        BoundsInt bounds = tilemap.cellBounds;
        List<Vector3Int> tilesToBreak = new List<Vector3Int>();

        // Собираем только тайлы, которые реально существуют (игнорируем пустые)
        for (int y = bounds.yMin; y <= bounds.yMax; y++)
        {
            for (int x = bounds.xMin; x <= bounds.xMax; x++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);
                if (tilemap.HasTile(pos))
                {
                    tilesToBreak.Add(pos);
                }
            }
        }

        // Сортируем по X (слева направо), затем по Y (снизу вверх)
        tilesToBreak.Sort((a, b) =>
        {
            if (a.y == b.y)
                return a.x.CompareTo(b.x); // по X слева направо
            else
                return b.y.CompareTo(a.y); // сверху вниз, если нужно (можно поменять на a.y.CompareTo(b.y) для снизу вверх)
        });

        // Удаляем тайлы по одному с задержкой
        foreach (Vector3Int pos in tilesToBreak)
        {
            tilemap.SetTile(pos, null);
            yield return new WaitForSeconds(delayBetweenBreaks);
        }
    }

    void OnDrawGizmos()
    {
        if (Camera.current != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position, detectionSize);
        }
    }
}
