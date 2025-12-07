using TMPro;
using UnityEngine;

public class CurrencyHUD : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI currencyText;

    private void Start()
    {
        UpdateCurrencyDisplay();

        CurrencyManager.Instance.OnCurrencyChanged += UpdateCurrencyDisplay;
    }

    private void UpdateCurrencyDisplay()
    {
        int current = CurrencyManager.Instance.GetCurrency();
        currencyText.text = current.ToString();
    }
}
