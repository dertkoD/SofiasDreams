using UnityEngine;
using Zenject;

public class BonfireInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] string bonfireId = "bonfire_01";
    [SerializeField] Transform checkpoint;

    IBonfireService _bonfire;

    [Inject]
    public void Construct(IBonfireService bonfire) => _bonfire = bonfire;

    void Awake()
    {
        if (checkpoint == null)
            checkpoint = transform;
    }

    public bool CanInteract => true;

    public string PromptText => _bonfire != null && _bonfire.IsResting
        ? "Press F to leave"
        : "Press F to rest";

    public void Interact(Transform interactor)
    {
        _bonfire.ToggleRest(bonfireId, checkpoint.position);
    }
}