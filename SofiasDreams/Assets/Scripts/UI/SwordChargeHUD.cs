using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class SwordChargeHUD : MonoBehaviour
{
    [Header("Root (entire sword charge panel)")]
    [SerializeField] GameObject root;

    [Header("Charge bar (Image with Filled type, Horizontal, Left origin)")]
    [SerializeField] Image chargeBar;

    [Header("Shrink animation")]
    [SerializeField] float shrinkSpeed = 5f;

    SignalBus _bus;
    float _targetFill;
    float _displayFill;

    [Inject]
    public void Construct(SignalBus bus)
    {
        _bus = bus;
        _bus.Subscribe<WeaponSwitched>(OnWeaponSwitched);
        _bus.Subscribe<SwordChargeChanged>(OnChargeChanged);

        SetVisible(false);
        SetFill(0f);
    }

    void OnDestroy()
    {
        _bus?.TryUnsubscribe<WeaponSwitched>(OnWeaponSwitched);
        _bus?.TryUnsubscribe<SwordChargeChanged>(OnChargeChanged);
    }

    void OnWeaponSwitched(WeaponSwitched s)
    {
        bool show = s.weapon == WeaponType.Sword;
        SetVisible(show);
        if (!show)
        {
            _targetFill = 0f;
            _displayFill = 0f;
            SetFill(0f);
        }
    }

    void OnChargeChanged(SwordChargeChanged s)
    {
        _targetFill = s.progress;
    }

    void Update()
    {
        if (!chargeBar) return;

        if (_targetFill > _displayFill)
            _displayFill = _targetFill;
        else if (_displayFill > _targetFill)
            _displayFill = Mathf.MoveTowards(_displayFill, _targetFill, shrinkSpeed * Time.deltaTime);

        SetFill(_displayFill);
    }

    void SetVisible(bool visible)
    {
        if (root) root.SetActive(visible);
    }

    void SetFill(float fill)
    {
        if (chargeBar) chargeBar.fillAmount = fill;
    }
}
