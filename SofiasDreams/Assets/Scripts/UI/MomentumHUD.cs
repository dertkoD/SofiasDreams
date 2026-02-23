using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class MomentumHUD : MonoBehaviour
{
    [Header("Root (entire momentum panel)")]
    [SerializeField] GameObject root;

    [Header("Level containers (yellow rectangles, size = 3)")]
    [SerializeField] GameObject[] levelContainers;

    [Header("Segments (blue squares, 5 per level, total 15)\n" +
            "Order: [0-4] = level 1, [5-9] = level 2, [10-14] = level 3")]
    [SerializeField] GameObject[] segments;

    [Header("Timer circle (Image with Filled type)")]
    [SerializeField] Image timerCircle;

    [SerializeField] int segmentsPerLevel = 5;

    SignalBus _bus;

    [Inject]
    public void Construct(SignalBus bus)
    {
        _bus = bus;
        _bus.Subscribe<WeaponSwitched>(OnWeaponSwitched);
        _bus.Subscribe<MomentumChanged>(OnMomentumChanged);

        SetVisible(false);
        Refresh(0);
    }

    void OnDestroy()
    {
        _bus?.TryUnsubscribe<WeaponSwitched>(OnWeaponSwitched);
        _bus?.TryUnsubscribe<MomentumChanged>(OnMomentumChanged);
    }

    void OnWeaponSwitched(WeaponSwitched s)
    {
        bool show = s.weapon == WeaponType.Dagger;
        SetVisible(show);
        if (!show) Refresh(0);
    }

    void OnMomentumChanged(MomentumChanged s)
    {
        Refresh(s.segments);
        if (timerCircle) timerCircle.fillAmount = s.circleFill;
    }

    void SetVisible(bool visible)
    {
        if (root) root.SetActive(visible);
    }

    void Refresh(int activeSegments)
    {
        for (int i = 0; i < segments.Length; i++)
        {
            if (segments[i])
                segments[i].SetActive(i < activeSegments);
        }

        for (int lvl = 0; lvl < levelContainers.Length; lvl++)
        {
            if (!levelContainers[lvl]) continue;

            int first = lvl * segmentsPerLevel;
            bool hasAny = activeSegments > first;
            levelContainers[lvl].SetActive(hasAny);
        }
    }
}
