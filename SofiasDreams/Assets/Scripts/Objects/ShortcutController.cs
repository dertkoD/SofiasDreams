using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ShortcutController : MonoBehaviour
{
    [Header("Assign all colliders under logic")]
    [SerializeField] private Collider2D[] colliders;

    [Header("Assign the visual sprite to destroy")]
    [SerializeField] private GameObject visualObject;
    
    [Header("Light")]
    [SerializeField] Light2D spotLight2D;

    public void DestroyShortcut()
    {
        foreach (var col in colliders)
        {
            if (col != null) Destroy(col);
        }
        if (visualObject != null) Destroy(visualObject);
        if (spotLight2D != null)spotLight2D.enabled = false;
    }
}
