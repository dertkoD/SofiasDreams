using UnityEngine;

public class PlayerCollisionHandler : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy") && !gameObject.CompareTag("Weapon"))
        {
            PlayerHealth.Instance.TakeDamage(1);
        }
    }
}
    
