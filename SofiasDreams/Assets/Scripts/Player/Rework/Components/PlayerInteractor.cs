using UnityEngine;

[DisallowMultipleComponent]
public class PlayerInteractor : MonoBehaviour
{
    [Header("Scan")]
    [SerializeField] float radius = 1.2f;
    [SerializeField] LayerMask interactableMask = ~0;
    [SerializeField] Transform originOverride; // optional

    readonly Collider2D[] _hits = new Collider2D[24];

    public bool TryInteract(Transform interactor)
    {
        if (interactor == null) interactor = transform;

        Vector2 origin = originOverride ? (Vector2)originOverride.position : (Vector2)transform.position;

        int count = Physics2D.OverlapCircleNonAlloc(origin, radius, _hits, interactableMask);
        if (count <= 0) return false;

        IInteractable best = null;
        float bestD2 = float.PositiveInfinity;

        for (int i = 0; i < count; i++)
        {
            var col = _hits[i];
            if (!col) continue;

            var it = FindInteractable(col);
            if (it == null) continue;
            if (!it.CanInteract) continue;

            if (it is not MonoBehaviour mb) continue;

            float d2 = ((Vector2)mb.transform.position - origin).sqrMagnitude;
            if (d2 < bestD2)
            {
                bestD2 = d2;
                best = it;
            }
        }

        if (best == null) return false;

        best.Interact(interactor);
        return true;
    }

    static IInteractable FindInteractable(Collider2D col)
    {
        // Try same object
        foreach (var mb in col.GetComponents<MonoBehaviour>())
            if (mb is IInteractable it) return it;

        // Try parent chain
        var t = col.transform;
        while (t != null)
        {
            foreach (var mb in t.GetComponents<MonoBehaviour>())
                if (mb is IInteractable it) return it;
            t = t.parent;
        }

        // Try children
        foreach (var mb in col.GetComponentsInChildren<MonoBehaviour>(true))
            if (mb is IInteractable it) return it;

        return null;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Vector3 p = originOverride ? originOverride.position : transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(p, radius);
    }
#endif
}