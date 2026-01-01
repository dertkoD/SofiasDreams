using UnityEngine;

public class UnlockGrapplePointInteractable : MonoBehaviour, IInteractable
{
    [Header("Target Lock")]
    [SerializeField] GrapplePointLock targetLock;

    [Header("Target Visual")]
    [SerializeField] SpriteRenderer targetSpriteRenderer;
    [SerializeField] Sprite unlockedGrapple;

    [Header("Self Visual (Child)")]
    [SerializeField] SpriteRenderer childSpriteRenderer;
    [SerializeField] Sprite unlockedChildSprite;

    [Header("Behavior")]
    [SerializeField] bool oneShot = true;

    [Header("Hint")]
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

        // Change grapple point sprite
        if (targetSpriteRenderer != null && unlockedGrapple != null)
            targetSpriteRenderer.sprite = unlockedGrapple;

        // Change this object's child sprite
        if (childSpriteRenderer != null && unlockedChildSprite != null)
            childSpriteRenderer.sprite = unlockedChildSprite;

        _used = true;

        hint?.DisableForeverFade(destroyHintOnUse);
    }
}
