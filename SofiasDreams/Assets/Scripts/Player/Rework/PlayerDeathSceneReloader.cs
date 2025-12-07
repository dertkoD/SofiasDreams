using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

public class PlayerDeathSceneReloader : IInitializable, ITickable, IDisposable
{
    readonly SignalBus _bus;
    readonly float _reloadDelay;

    bool _pendingReload;
    float _timer;

    public PlayerDeathSceneReloader(SignalBus bus, [Inject(Optional = true)] float reloadDelay = 1.0f)
    {
        _bus = bus;
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
