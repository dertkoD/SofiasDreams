using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

public class PlayerDeathSceneReloader : IInitializable, ITickable, IDisposable
{
    readonly SignalBus _bus;
    readonly float _reloadDelay;
    readonly IBonfireService _bonfire;

    bool _pendingReload;
    float _timer;

    public PlayerDeathSceneReloader(SignalBus bus, 
        [Inject(Optional = true)] IBonfireService bonfire,
        [Inject(Optional = true)] float reloadDelay = 1.0f)
    {
        _bus = bus;
        _bonfire = bonfire;
        _reloadDelay = reloadDelay;
    }

    public void Initialize()
    {
        _bus.Subscribe<Died>(OnPlayerDied);
    }

    public void Dispose()
    {
        _bus.TryUnsubscribe<Died>(OnPlayerDied);
    }

    void OnPlayerDied(Died _)
    {
        // Check for checkpoint via PlayerPrefs to avoid dependency injection issues if this class isn't properly bound
        bool hasCheckpoint = PlayerPrefs.HasKey("checkpoint.bonfireId");
        
        // If we have a checkpoint, the Bonfire system (BonfireRespawnOnDeath) 
        // will handle respawn. Don't reload the scene.
        if (hasCheckpoint)
            return;

        if (_pendingReload)
            return;

        _pendingReload = true;
        _timer = _reloadDelay;
    }

    public void Tick()
    {
        if (!_pendingReload)
            return;

        _timer -= Time.deltaTime;
        if (_timer <= 0f)
        {
            var scene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(scene.buildIndex);
        }
    }
}
