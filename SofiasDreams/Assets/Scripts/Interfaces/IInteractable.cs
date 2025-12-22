using UnityEngine;

public interface IInteractable
{
    /// <summary>
    /// Can the player interact with this object right now?
    /// Used by PlayerInteractor before calling Interact().
    /// </summary>
    bool CanInteract { get; }

    /// <summary>
    /// Optional text for UI prompts (e.g. "Press F").
    /// Return null or empty string if no prompt should be shown.
    /// </summary>
    string PromptText { get; }

    /// <summary>
    /// Execute the interaction.
    /// The interactor Transform is passed for context if needed
    /// (facing direction, position, inventory, etc.).
    /// </summary>
    void Interact(Transform interactor);
}