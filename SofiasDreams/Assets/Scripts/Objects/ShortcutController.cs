using UnityEngine;

public class ShortcutController : MonoBehaviour
{
    [Header("Assign all colliders under logic")]
    [SerializeField] private Collider2D[] colliders;

    [Header("Assign the visual sprite to destroy")]
    [SerializeField] private GameObject visualObject;

    public void DestroyShortcut()
    {
        foreach (var col in colliders)
        {
            if (col != null) Destroy(col);
        }
        if (visualObject != null) Destroy(visualObject);
    }
}
