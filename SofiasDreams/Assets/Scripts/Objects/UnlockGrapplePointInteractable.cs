using UnityEngine;

public class UnlockGrapplePointInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] GrapplePointLock targetLock;
    [SerializeField] bool oneShot = true;

    bool _used;

    public bool CanInteract =>
        targetLock != null &&
        targetLock.IsLocked &&
        !(oneShot && _used);

    public string PromptText => CanInteract ? "Press F" : "";

    public void Interact(Transform interactor)
    {
        if (!CanInteract) return;

        targetLock.Unlock();
        _used = true;

        // optional: animation/sfx/vfx
    }
}
