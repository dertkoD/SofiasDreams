using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BossPlatformBreaker : MonoBehaviour
{
    [SerializeField] Tilemap _tilemap;
    [SerializeField] float _breakDelay = 0.1f;
    [SerializeField] bool _triggered;

    public void BreakAll()
    {
        if (_triggered) return;
        _triggered = true;
        StartCoroutine(BreakRoutine());
    }

    IEnumerator BreakRoutine()
    {
        if (_tilemap == null) yield break;

        BoundsInt bounds = _tilemap.cellBounds;
        List<Vector3Int> tiles = new List<Vector3Int>();

        for (int x = bounds.xMin; x <= bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y <= bounds.yMax; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);
                if (_tilemap.HasTile(pos))
                {
                    tiles.Add(pos);
                }
            }
        }

        // Sort by X to break from left to right (or any other pattern)
        tiles.Sort((a, b) => a.x.CompareTo(b.x));

        foreach (var pos in tiles)
        {
            _tilemap.SetTile(pos, null);
            yield return new WaitForSeconds(_breakDelay);
        }
        
        // Disable collider if any remaining attached to this object (e.g. composite)
        var col = GetComponent<Collider2D>();
        if (col) col.enabled = false;
    }
}
