using UnityEngine;
using Zenject;

public class PlayerDissolveVfxBridge : MonoBehaviour
{
    [SerializeField] private SpriteDissolveController dissolve;
    [SerializeField] private PlayerDissolveBundleSO bundle;

    private SignalBus _bus;
    private bool _deathVfxPlaying;

    [Inject]
    void Construct(SignalBus bus) => _bus = bus;

    private void Awake()
    {
        if (!dissolve) dissolve = GetComponentInChildren<SpriteDissolveController>();
    }

    private void OnEnable()
    {
        _bus.Subscribe<Died>(OnDied);

        _bus.TrySubscribe<PlayerRespawnedAtBonfire>(OnRespawnedAtBonfire);

        _bus.TrySubscribe<PlayerSpawned>(OnPlayerSpawned);
    }

    private void OnDisable()
    {
        _bus.TryUnsubscribe<Died>(OnDied);
        _bus.TryUnsubscribe<PlayerRespawnedAtBonfire>(OnRespawnedAtBonfire);
        _bus.TryUnsubscribe<PlayerSpawned>(OnPlayerSpawned);
    }

    private void OnDied(Died _)
    {
        if (_deathVfxPlaying) return;
        _deathVfxPlaying = true;

        dissolve.Play(bundle.death, () =>
        {
            _deathVfxPlaying = false;
            _bus.Fire(new PlayerDeathVfxFinished());
        });
    }

    private void OnRespawnedAtBonfire(PlayerRespawnedAtBonfire _)
    {
        PlayRespawnVfx();
    }

    private void OnPlayerSpawned(PlayerSpawned s)
    {
        Debug.Log($"[VFX_DEBUG] OnPlayerSpawned received. Facade matches: {s.facade != null && s.facade.gameObject == gameObject}");
        if (s.facade != null && s.facade.gameObject == gameObject)
        {
             PlayRespawnVfx();
        }
    }

    private void PlayRespawnVfx()
    {
        if (!bundle) 
        {
            Debug.LogError("[VFX_DEBUG] Bundle is missing!");
            return;
        }
        if (!bundle.respawn)
        {
            Debug.LogError("[VFX_DEBUG] Bundle.Respawn setting is missing!");
            return;
        }

        Debug.Log($"[VFX_DEBUG] Playing Respawn VFX. Start: {bundle.respawn.startAmount}, End: {bundle.respawn.endAmount}");

        // Ensure sprite renderer is enabled and material props are set immediately
        dissolve.ApplyInstant(bundle.respawn, bundle.respawn.startAmount, bundle.respawn.outlineStartThickness);
        dissolve.Play(bundle.respawn);
    }
}
