using UnityEngine;
using UnityEngine.VFX;

public class SmokeTetherController : MonoBehaviour
{
    [SerializeField] private Transform headAnchor;
    [SerializeField] private Transform bodyAnchor;
    [SerializeField] private VisualEffect vfx;

    private static readonly int HeadPosID = Shader.PropertyToID("StartPos");
    private static readonly int BodyPosID = Shader.PropertyToID("EndPos");

    void LateUpdate()
    {
        if (headAnchor == null || bodyAnchor == null || vfx == null) return;

        Vector3 head = headAnchor.position;
        Vector3 body = bodyAnchor.position;

        vfx.SetVector3(HeadPosID, head);
        vfx.SetVector3(BodyPosID, body);
    }
}