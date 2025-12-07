using System;
using UnityEngine;

public class BonfireInteraction : MonoBehaviour
{
    private bool playerInRange;
    
    void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            if (BonfireManager.Instance.IsAtBonfire)
                BonfireManager.Instance.ExitBonfire();
            else 
                BonfireManager.Instance.ActivateBonfire(transform.position);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            playerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            playerInRange = false;
    }
}
