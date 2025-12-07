using UnityEngine;

public class LedgeGuard2D : MonoBehaviour
{
    [Header("Probe")]
    public LayerMask groundLayers;                // если 0 — возьмём fallback из вызова
    public Vector2 probeOffset = new Vector2(0f, 0.1f);
    [Min(0f)] public float probeForward = 0.35f;
    [Min(0f)] public float probeDown = 0.6f;

    [Header("Facing source (для гизмосов)")]
    [Tooltip("Если задан — знак берётся из lossyScale.x этого трансформа в редакторе. В рантайме мозг может передавать знак через SetFacingSign().")]
    public Transform facingTransform;

    [Header("Gizmos")]
    public bool drawGizmos = true;
    public Color gizmoColor = Color.magenta;

    // Рантайм-знак, который может проставлять мозг (для гизмосов во время Play)
    int _runtimeFacingSign = +1;

    public void SetFacingSign(int sign)
    {
        _runtimeFacingSign = sign >= 0 ? +1 : -1;
    }

    public bool IsLedgeAhead(Vector2 origin, int facingSign, LayerMask fallbackLayers = default)
    {
        LayerMask lm = (groundLayers.value != 0) ? groundLayers : fallbackLayers;
        Vector2 o = origin + probeOffset + Vector2.right * (facingSign >= 0 ? +1 : -1) * probeForward;
        return !Physics2D.Raycast(o, Vector2.down, probeDown, lm);
    }

    int SignFromScale(Transform t)
    {
        if (!t) return +1;
        float sx = t.lossyScale.x;
        if (Mathf.Approximately(sx, 0f)) sx = 1f;
        return sx >= 0f ? +1 : -1;
    }

    void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;

        int sign = Application.isPlaying
            ? _runtimeFacingSign                              // В Play используем знак от мозга
            : (facingTransform ? SignFromScale(facingTransform)
                : SignFromScale(transform));    // В редакторе — по трансформу

        Vector3 o = transform.position + (Vector3)probeOffset + Vector3.right * sign * probeForward;

        Gizmos.color = gizmoColor;
        Gizmos.DrawLine(o, o + Vector3.down * probeDown);
        Gizmos.DrawSphere(o, 0.03f);
    }
}
