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
    }

    public void Dispose()
    {
        _bus.TryUnsubscribe<EnemyHit>(OnEnemyHit);
        _bus.TryUnsubscribe<TookDamage>(OnTookDamage);
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

        float rate = _cfg ? _cfg.decayRate : 1f;
        float prev = _segments;
        _segments = Mathf.Max(0f, _segments - rate * Time.deltaTime);

        int prevLevel = Mathf.FloorToInt(prev) / SegmentsPerLevel;
        if (Level < prevLevel)
            Debug.Log($"[Momentum] Decay → Level {Level} ({Segments}/{MaxSegments})");

        FireChanged();
    }

    public void OnParrySuccess()
    {
        int add = _cfg ? _cfg.parrySegments : 5;
        AddSegments(add, "Parry");
    }

    void OnEnemyHit(EnemyHit e)
    {
        if (_weapon.CurrentWeapon != WeaponType.Dagger) return;

        int add = e.isBackstab
            ? (_cfg ? _cfg.backstabSegments : 1)
            : (_cfg ? _cfg.normalHitSegments : 1);

        string src = e.isBackstab ? "Backstab" : "Hit";
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
