using UnityEngine;

public class ShortcutDamageable : MonoBehaviour, IDamageable
{
    [SerializeField] private ShortcutController shortcutController;

    [Tooltip("If true, the weapon must be to the LEFT of this shortcut to allow destruction.")]
    [SerializeField] private bool requireHitFromLeft = true;

    private bool _isDestroyed = false;

    // Shortcuts don't really have HP, but we must implement the interface
    public bool IsAlive => !_isDestroyed;

    public void ApplyDamage(int amount, Vector2 hitPoint, Vector2 hitNormal, GameObject source)
    {
        if (_isDestroyed)
            return;

        if (shortcutController == null)
        {
            Debug.LogWarning("ShortcutController reference missing on ShortcutDamageable.");
            return;
        }

        float shortcutX = transform.position.x;
        float sourceX   = source.transform.position.x;

        bool hitFromLeft = sourceX < shortcutX;

        if (hitFromLeft == requireHitFromLeft)
        {
            // Correct side → destroy shortcut
            _isDestroyed = true;
            shortcutController.DestroyShortcut();
            Debug.Log("Shortcut destroyed by weapon from correct side.");
        }
        else
        {
            Debug.Log("Shortcut hit from wrong side, not destroying.");
        }
    }
}
