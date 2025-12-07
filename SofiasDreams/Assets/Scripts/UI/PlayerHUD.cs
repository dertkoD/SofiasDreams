using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class PlayerHUD : MonoBehaviour
{
    [Header("Image Elements")]
    [SerializeField] private Image heals;
    [SerializeField] private Image hpbar;

    [Header("Powershot UI")]
    [SerializeField] private Image powershotIcon;
    [SerializeField] private Image powershotCooldown;
    [SerializeField] private Color readyColor   = Color.white;
    [SerializeField] private Color cooldownColor = new Color(1f, 1f, 1f, 0.5f);

    [Header("Sprites")]
    [SerializeField] private Sprite threeHeals;
    [SerializeField] private Sprite twoHeals;
    [SerializeField] private Sprite oneHeal;
    [SerializeField] private Sprite zeroHeals;
    [SerializeField] private Sprite fiveHP;
    [SerializeField] private Sprite fourHP;
    [SerializeField] private Sprite threeHP;
    [SerializeField] private Sprite twoHP;
    [SerializeField] private Sprite oneHP;
    [SerializeField] private Sprite zeroHP;

    SignalBus _bus;
    Health _health;          
    Healer _healer;
    PlayerRangedAbility _ranged;

    [Inject]
    public void Construct(SignalBus bus) => _bus = bus;

    void OnEnable()
    {
        _bus.Subscribe<PlayerSpawned>(OnPlayerSpawned);
        _bus.Subscribe<HealChargesChanged>(OnHealChargesChanged);
        _bus.Subscribe<TookDamage>(_ => UpdateHP());
        _bus.Subscribe<HealFinished>(_ => UpdateHP());

        RefreshHP(0);
        RefreshHeals(0);
        HandleReadyChanged(true);
        HandleCooldownTick(0f, 1f);
    }

    void OnDisable()
    {
        _bus.TryUnsubscribe<PlayerSpawned>(OnPlayerSpawned);
        _bus.TryUnsubscribe<HealChargesChanged>(OnHealChargesChanged);
        _bus.TryUnsubscribe<TookDamage>(_ => UpdateHP());      
        _bus.TryUnsubscribe<HealFinished>(_ => UpdateHP());

        if (_health != null)
        {
            _health.OnHealthChanged -= UpdateHP;
            _health = null;
        }

        if (_ranged != null)
        {
            _ranged.OnCooldownTick -= HandleCooldownTick;
            _ranged.OnReadyChanged -= HandleReadyChanged;
            _ranged = null;
        }
    }

    void OnPlayerSpawned(PlayerSpawned s)
    {
        if (_health != null)
            _health.OnHealthChanged -= UpdateHP;
        if (_ranged != null)
        {
            _ranged.OnCooldownTick -= HandleCooldownTick;
            _ranged.OnReadyChanged -= HandleReadyChanged;
        }

        _health = s.facade.Health;
        _healer = s.facade.Healer;
        _ranged = s.facade.RangedAbility;

        if (_health != null)
        {
            _health.OnHealthChanged += UpdateHP;
            UpdateHP();
        }
        else
        {
            RefreshHP(0);
        }

        if (_healer != null)
        {
            RefreshHeals(_healer.CurrentCharges);
        }
        else
        {
            RefreshHeals(0);
        }

        if (_ranged != null)
        {
            HandleCooldownTick(_ranged.CooldownRemaining, _ranged.CooldownDuration);
            HandleReadyChanged(!_ranged.IsOnCooldown);
            _ranged.OnCooldownTick += HandleCooldownTick;
            _ranged.OnReadyChanged += HandleReadyChanged;
        }
        else
        {
            HandleReadyChanged(true);
            HandleCooldownTick(0f, 1f);
        }
    }

    void OnHealChargesChanged(HealChargesChanged s)
    {
        RefreshHeals(s.current);
    }

    void UpdateHP()
    {
        if (_health == null) { RefreshHP(0); return; }
        RefreshHP(_health.CurrentHP);
    }

    void RefreshHP(int current)
    {
        if (!hpbar) return;
        hpbar.sprite = current switch
        {
            0 => zeroHP,
            1 => oneHP,
            2 => twoHP,
            3 => threeHP,
            4 => fourHP,
            _ => fiveHP
        };
    }

    void RefreshHeals(int current)
    {
        if (!heals) return;
        heals.sprite = current switch
        {
            0 => zeroHeals,
            1 => oneHeal,
            2 => twoHeals,
            _ => threeHeals
        };
    }

    void HandleCooldownTick(float remaining, float duration)
    {
        if (!powershotCooldown || duration <= 0f) return;
        powershotCooldown.fillAmount = Mathf.Clamp01(remaining / duration);
        if (powershotIcon) powershotIcon.color = remaining > 0f ? cooldownColor : readyColor;
    }

    void HandleReadyChanged(bool ready)
    {
        if (powershotIcon) powershotIcon.color = ready ? readyColor : cooldownColor;
        if (powershotCooldown && ready) powershotCooldown.fillAmount = 0f;
    }
}
