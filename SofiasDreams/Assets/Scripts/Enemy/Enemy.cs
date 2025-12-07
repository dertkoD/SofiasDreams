using System;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            var feedback = other.GetComponent<PlayerDamageFeedback>();
            feedback.ApplyDamageWithKnockback(1, transform.position);
            Debug.Log("Enemy hit player: " + other.gameObject.name);
        }    
    }
}
