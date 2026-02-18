using UnityEngine;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent]
public class GrapplePoint : MonoBehaviour
{
    [Header("Greybox")]
    public float gizmoSize = 0.15f;
    public Color gizmoColor = new Color(0.6f, 0.6f, 0.6f, 1f);

    [Header("Light")]
    [SerializeField] Light2D spotLight2D;

    bool _isCandidate; 
    bool _isLatched;   

    void Reset()
    {
        if (!spotLight2D) spotLight2D = GetComponentInChildren<Light2D>(true);
    }

    void Awake()
    {
        if (!spotLight2D) spotLight2D = GetComponentInChildren<Light2D>(true);
        ApplyLight();
    }

    public void SetCandidate(bool value)
    {
        _isCandidate = value;
        ApplyLight();
    }

    public void SetLatched(bool value)
    {
        _isLatched = value;
        ApplyLight();
    }

    void ApplyLight()
    {
        if (!spotLight2D) return;
        spotLight2D.enabled = _isLatched || _isCandidate;
    }
    
    /*private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawCube(transform.position, Vector3.one * gizmoSize);
    }*/
}
