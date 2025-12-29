using UnityEngine;

public class UnlockGrapplePointInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] GrapplePointLock targetLock;
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] Sprite unlockedGrapple;
    [SerializeField] bool oneShot = true;

    bool _used;

    public bool CanInteract =>
        targetLock != null &&
        targetLock.IsLocked &&
        !(oneShot && _used);

    public string PromptText => CanInteract ? "Press F" : "";

    public void Interact(Transform interactor)
    {
        Debug.Log($"[GrappleUnlock] Interact called. targetLock={(targetLock ? targetLock.name : "NULL")} locked(before)={targetLock?.IsLocked}");

        if (!CanInteract) return;
        
        targetLock.Unlock();
        spriteRenderer.sprite = unlockedGrapple;
        
        Debug.Log($"[GrappleUnlock] after Unlock. locked(after)={targetLock?.IsLocked}");
        
        _used = true;

        // optional: animation/sfx/vfx
    }
}
