using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class BossPlatformBreaker : MonoBehaviour
{
    [Header("Parts (optional)")]
    [SerializeField] private List<GameObject> _platformParts;

    [Header("Timing")]
    [SerializeField] private float _breakDelay = 0.1f;

    [Header("Dissolve")]
    [SerializeField] private DissolveVfxSettingsSO _dissolveSettings;
    [SerializeField] private bool _sequential = false;

    [Header("After")]
    [SerializeField] private bool _destroyPartsObjects = false;
    [SerializeField] private float _destroyRootDelay = 0.1f;

    [SerializeField] private bool _triggered;
    
    [Inject] SignalBus _bus;

    public void BreakAll()
    {
        if (_triggered) return;
        _triggered = true;
        
        _bus.Fire<BossFloorBrokenSignal>();
        
        StartCoroutine(BreakRoutine());
    }

    private IEnumerator BreakRoutine()
    {
        EnsurePartsList();

        DisableAllColliders(gameObject);

        if (_platformParts == null || _platformParts.Count == 0)
        {
            yield return DissolveAndRemove(gameObject, isRoot: true);
            yield break;
        }

        if (_sequential)
        {
            foreach (var part in _platformParts)
            {
                if (!part) continue;
                yield return DissolveAndRemove(part, isRoot: false);
                yield return new WaitForSeconds(_breakDelay);
            }
        }
        else
        {
            float lastStartDelay = 0f;

            foreach (var part in _platformParts)
            {
                if (!part) continue;

                StartCoroutine(DissolveAndRemove(part, isRoot: false));
                yield return new WaitForSeconds(_breakDelay);
                lastStartDelay += _breakDelay;
            }

            float extra = (_dissolveSettings ? _dissolveSettings.duration : 0.0f) + 0.05f;
            yield return new WaitForSeconds(extra);
        }

        Destroy(gameObject, _destroyRootDelay);
    }

    private void EnsurePartsList()
    {
        if (_platformParts != null && _platformParts.Count > 0) return;

        if (transform.childCount <= 0)
        {
            _platformParts = new List<GameObject>(0);
            return;
        }

        _platformParts = new List<GameObject>();
        foreach (Transform child in transform)
            _platformParts.Add(child.gameObject);

        _platformParts.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
    }

    private IEnumerator DissolveAndRemove(GameObject go, bool isRoot)
    {
        if (!go) yield break;

        DisableAllColliders(go);

        if (_dissolveSettings == null)
        {
            go.SetActive(false);
            if (isRoot) Destroy(gameObject, _destroyRootDelay);
            yield break;
        }

        var dissolve = go.GetComponent<SpriteDissolveController>();
        if (!dissolve) dissolve = go.AddComponent<SpriteDissolveController>();

        bool finished = false;
        dissolve.Play(_dissolveSettings, () => finished = true);

        while (!finished) yield return null;

        if (!isRoot)
        {
            if (_destroyPartsObjects) Destroy(go);
            else go.SetActive(false);
        }
        else
        {
            Destroy(gameObject, _destroyRootDelay);
        }
    }

    private static void DisableAllColliders(GameObject go)
    {
        var cols = go.GetComponentsInChildren<Collider2D>(includeInactive: true);
        for (int i = 0; i < cols.Length; i++)
            cols[i].enabled = false;
    }
}
