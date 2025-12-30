using UnityEngine;
using Zenject;
public class UnlockDashInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] bool oneShot = true;
    [SerializeField] WorldHintTextFade hint;
    [SerializeField] bool destroyHintOnUse = true;

    IPlayerAbilities _abilities;
    SignalBus _bus;
    bool _used;

    [Inject]
    void Construct(IPlayerAbilities abilities, SignalBus bus)
    {
        _abilities = abilities;
        _bus = bus;
    }

    public bool CanInteract =>
        _abilities != null &&
        !_abilities.HasDash &&
        !(oneShot && _used);

    public string PromptText => CanInteract ? "Press F to learn Dash" : "";

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
        
        _abilities.GrantDash();
        
        _bus.Fire(new DashUnlocked());

        _used = true;

        hint?.DisableForeverFade(destroyHintOnUse);

    }
}
