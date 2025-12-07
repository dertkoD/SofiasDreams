using UnityEngine;

[DisallowMultipleComponent]
public class GrapplePoint : MonoBehaviour
{
    [Header("Greybox")]
    public float gizmoSize = 0.15f;
    public Color gizmoColor = new Color(0.6f, 0.6f, 0.6f, 1f);

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawCube(transform.position, Vector3.one * gizmoSize);
    }
}
