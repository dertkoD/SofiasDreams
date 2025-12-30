using Unity.Cinemachine;
using UnityEngine;

public class CameraZoneConfinerSwap : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private CinemachineConfiner2D confiner;
    [SerializeField] private CinemachineCamera vcam; 

    [Header("Bounds")]
    [SerializeField] private Collider2D boundsClosed;
    [SerializeField] private Collider2D boundsOpen;

    [Header("Tags")]
    [SerializeField] private string playerTag = "Player";

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        Apply(boundsOpen);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        Apply(boundsClosed);
    }

    private void Apply(Collider2D shape)
    {
        confiner.BoundingShape2D = shape;

        confiner.InvalidateBoundingShapeCache();

        if (vcam != null) vcam.PreviousStateIsValid = false;
    }
}
