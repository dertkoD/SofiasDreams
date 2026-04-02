using System;
using UnityEngine;
using Zenject;

public class DaggerMomentum : MonoBehaviour, IInitializable, IDisposable, ITickable
{
    SignalBus _bus;
    DaggerAttackConfig _cfg;
    IWeaponManager _weapon;

    float _segments;
    float _decayTimer;

    AttackMode _currentAttackMode;
    bool _inDaggerAttack;

    int MaxSegments => _cfg ? _cfg.segmentsPerLevel * _cfg.maxLevels : 15;
    int SegmentsPerLevel => _cfg ? _cfg.segmentsPerLevel : 5;

    public int Segments => Mathf.FloorToInt(_segments);
    public int Level => Mathf.Clamp(Segments / SegmentsPerLevel, 0, _cfg ? _cfg.maxLevels : 3);
    public float SpeedMultiplier => 1f + Level * (_cfg ? _cfg.speedBonusPerLevel : 0.2f);

    [Inject]
    void Construct(SignalBus bus,
        IWeaponManager weapon,
        [Inject(Optional = true)] DaggerAttackConfig cfg = null)
    {
        _bus = bus;
        _weapon = weapon;
        _cfg = cfg;
    }

    public void Initialize()
    {
        _bus.Subscribe<EnemyHit>(OnEnemyHit);
        _bus.Subscribe<TookDamage>(OnTookDamage);
        _bus.Subscribe<AttackStarted>(OnAttackStarted);
        _bus.Subscribe<AttackFinished>(OnAttackFinished);
    }

    public void Dispose()
    {
        _bus.TryUnsubscribe<EnemyHit>(OnEnemyHit);
        _bus.TryUnsubscribe<TookDamage>(OnTookDamage);
        _bus.TryUnsubscribe<AttackStarted>(OnAttackStarted);
        _bus.TryUnsubscribe<AttackFinished>(OnAttackFinished);
    }

    public void Tick()
    {
        if (_weapon.CurrentWeapon != WeaponType.Dagger || _segments <= 0f) return;

        _decayTimer -= Time.deltaTime;

        if (_decayTimer > 0f)
        {
            FireChanged();
            return;
        }

        bool instant = _cfg && _cfg.instantFullDecay;

        if (instant)
        {
            int prevLevel = Level;
            _segments = 0f;
            _decayTimer = 0f;
            Debug.Log($"[Momentum] Instant decay → Level 0 (0/{MaxSegments}) was Level {prevLevel}");
            FireChanged();
            return;
        }

        float rate = _cfg ? _cfg.decayRate : 1f;
        float prev = _segments;
        _segments = Mathf.Max(0f, _segments - rate * Time.deltaTime);

        int prevLvl = Mathf.FloorToInt(prev) / SegmentsPerLevel;
        if (Level < prevLvl)
            Debug.Log($"[Momentum] Decay → Level {Level} ({Segments}/{MaxSegments})");

        FireChanged();
    }

    public void OnParrySuccess()
    {
        int add = _cfg ? _cfg.parrySegments : 5;
        AddSegments(add, "Parry");
    }

    static bool IsDaggerAirMode(AttackMode m) =>
        m is AttackMode.DaggerFlyUp or AttackMode.DaggerFlyDown;

    void OnAttackStarted(AttackStarted e)
    {
        if (IsDaggerMode(e.mode))
        {
            _currentAttackMode = e.mode;
            _inDaggerAttack = true;
        }
    }

    void OnAttackFinished(AttackFinished e)
    {
        if (IsDaggerMode(e.mode))
            _inDaggerAttack = false;
    }

    static bool IsDaggerMode(AttackMode m) =>
        m is AttackMode.DaggerCombo or AttackMode.DaggerSuper
            or AttackMode.DaggerFlyUp or AttackMode.DaggerFlyDown;

    void OnEnemyHit(EnemyHit e)
    {
        if (_weapon.CurrentWeapon != WeaponType.Dagger) return;

        int add;
        string src;

        if (_inDaggerAttack && IsDaggerAirMode(_currentAttackMode))
        {
            add = _cfg ? _cfg.airHitSegments : 1;
            src = "AirHit";
        }
        else if (_inDaggerAttack && _currentAttackMode == AttackMode.DaggerSuper)
        {
            add = _cfg ? _cfg.chargedHitSegments : 1;
            src = "ChargedHit";
        }
        else if (e.isBackstab)
        {
            add = _cfg ? _cfg.backstabSegments : 1;
            src = "Backstab";
        }
        else
        {
            add = _cfg ? _cfg.normalHitSegments : 1;
            src = "Hit";
        }

        AddSegments(add, src);
    }

    void OnTookDamage(TookDamage _)
    {
        if (_segments <= 0f) return;

        int prevLevel = Level;
        _segments = 0f;
        _decayTimer = 0f;
        Debug.Log($"[Momentum] Damage taken! Reset → Level 0 (0/{MaxSegments}) was Level {prevLevel}");
        FireChanged();
    }

    void AddSegments(int amount, string source)
    {
        int prevLevel = Level;
        _segments = Mathf.Min(_segments + amount, MaxSegments);
        ResetDecayTimer();

        Debug.Log($"[Momentum] +{amount} ({source}) → {Segments}/{MaxSegments} Level {Level} (x{SpeedMultiplier:F1})");

        if (Level > prevLevel)
            Debug.Log($"[Momentum] ★ Level UP! {prevLevel} → {Level}");

        FireChanged();
    }

    void ResetDecayTimer()
    {
        _decayTimer = _cfg ? _cfg.decayDelay : 5f;
    }

    float ComputeCircleFill()
    {
        if (_segments <= 0f) return 0f;

        float delay = _cfg ? _cfg.decayDelay : 5f;
        if (_decayTimer > 0f)
            return Mathf.Clamp01(_decayTimer / delay);

        return _segments - Mathf.Floor(_segments);
    }

    void FireChanged()
    {
        _bus.Fire(new MomentumChanged
        {
            segments = Segments,
            maxSegments = MaxSegments,
            level = Level,
            maxLevels = _cfg ? _cfg.maxLevels : 3,
            circleFill = ComputeCircleFill()
        });
    }
}
