using System.Collections.Generic;
using UnityEngine;

public class GameObjectPooler : MonoBehaviour
{
    public GameObject prefab;
    public int warmup = 0;
    private readonly Queue<GameObject> _q = new();

    private void Awake()
    {
        for (int i = 0; i < warmup; i++) _q.Enqueue(Create());
    }

    private GameObject Create()
    {
        var go = Instantiate(prefab, transform);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale    = Vector3.one;          // <- NEW
        go.SetActive(false);
        return go;
    }

    public GameObject Get(Vector3 pos, Quaternion rot)
    {
        var go = _q.Count > 0 ? _q.Dequeue() : Create();
        go.transform.SetParent(null, false);               // отцепили, local == world
        go.transform.SetPositionAndRotation(pos, rot);
        go.transform.localScale = Vector3.one;             // <- NEW: гарант 1:1
        go.SetActive(true);
        return go;
    }

    public void Return(GameObject go)
    {
        go.SetActive(false);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale    = Vector3.one;          // <- NEW
        _q.Enqueue(go);
    }
}
