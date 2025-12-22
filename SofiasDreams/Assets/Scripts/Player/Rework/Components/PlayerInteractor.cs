using System;
using UnityEngine;
using Zenject;

public class PlayerInteractor : MonoBehaviour
{
    IInteractable _current;
    public IInteractable Current => _current;

    void OnTriggerEnter2D(Collider2D other) => TrySet(other);
    void OnTriggerStay2D(Collider2D other)  => TrySet(other);

    void OnTriggerExit2D(Collider2D other)
    {
        var i = other.GetComponent<IInteractable>()
                ?? other.GetComponentInParent<IInteractable>()
                ?? other.GetComponentInChildren<IInteractable>();

        if (i != null && i == _current)
            _current = null;
    }

    void TrySet(Collider2D other)
    {
        var i = other.GetComponent<IInteractable>()
                ?? other.GetComponentInParent<IInteractable>()
                ?? other.GetComponentInChildren<IInteractable>();

       
        if (i == null) return;
        
        if (_current == null || !_current.CanInteract)
            _current = i;
    }
}
