using System;
using UnityEngine;

public class CurrencyPickUp : MonoBehaviour
{
    [SerializeField] private int value = 1;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            CurrencyManager.Instance.AddCurrency(value);
            Destroy(gameObject);
        }
    }
}
