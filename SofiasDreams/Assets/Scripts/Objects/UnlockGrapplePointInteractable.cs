using UnityEngine;

public class UnlockGrapplePointInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] GrapplePointLock targetLock;
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] Sprite unlockedGrapple;
    [SerializeField] bool oneShot = true;
    [SerializeField] WorldHintTextFade hint;
    [SerializeField] bool destroyHintOnUse = true;

    bool _used;

    public bool CanInteract =>
        targetLock != null &&
        targetLock.IsLocked &&
        !(oneShot && _used);

    public string PromptText => CanInteract ? "Press F" : "";

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (!CanInteract) return;

        hint?.Show();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        hint?.Hide();
    }

    public void Interact(Transform interactor)
    {
        if (!CanInteract) return;

        targetLock.Unlock();
        spriteRenderer.sprite = unlockedGrapple;
        _used = true;
        
        hint?.DisableForeverFade(destroyHintOnUse);
    }
}
