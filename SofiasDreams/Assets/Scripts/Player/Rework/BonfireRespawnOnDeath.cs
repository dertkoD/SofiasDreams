using Zenject;
using System;

public class BonfireRespawnOnDeath : IInitializable, IDisposable
{
    readonly SignalBus _bus;
    readonly IBonfireService _bonfire;

    public BonfireRespawnOnDeath(SignalBus bus, IBonfireService bonfire)
    {
        _bus = bus;
        _bonfire = bonfire;
    }

    public void Initialize()
    {
        _bus.Subscribe<Died>(OnDied);
    }

    public void Dispose()
    {
        _bus.Unsubscribe<Died>(OnDied);
    }

    void OnDied(Died _)
    {
        _bonfire.RespawnPlayerAtCheckpoint();
    }
}