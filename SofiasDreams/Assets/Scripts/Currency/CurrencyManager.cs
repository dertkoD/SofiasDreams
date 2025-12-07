using UnityEngine;
using UnityEngine.Events;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance;

    private int currentCurrency = 0;

    public event System.Action OnCurrencyChanged;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void AddCurrency(int amount)
    {
        currentCurrency += amount;
        OnCurrencyChanged?.Invoke();
        Debug.Log($"Currency: {currentCurrency}");
    }

    public int GetCurrency()
    {
        return currentCurrency;
    }
}
