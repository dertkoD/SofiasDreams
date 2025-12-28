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
        _bus.Subscribe<PlayerDeathVfxFinished>(OnDeathVfxFinished);
    }

    public void Dispose()
    {
        _bus.Unsubscribe<PlayerDeathVfxFinished>(OnDeathVfxFinished);
    }

    void OnDeathVfxFinished(PlayerDeathVfxFinished _)
    {
        _bonfire.RespawnPlayerAtCheckpoint();
    }
}