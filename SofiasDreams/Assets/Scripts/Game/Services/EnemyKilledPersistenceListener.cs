using System;
using UnityEngine;
using Zenject;

public class EnemyKilledPersistenceListener : IInitializable, IDisposable
{
    readonly SignalBus _bus;
    readonly IEnemyPersistenceService _persist;

    public EnemyKilledPersistenceListener(SignalBus bus, IEnemyPersistenceService persist)
    {
        _bus = bus;
        _persist = persist;
    }

    public void Initialize() => _bus.Subscribe<EnemyKilledSignal>(OnKilled);
    public void Dispose() => _bus.TryUnsubscribe<EnemyKilledSignal>(OnKilled);

    void OnKilled(EnemyKilledSignal s)
    {
        if (s.respawnMode != EnemyRespawnMode.PersistOnceKilled) return;
        if (string.IsNullOrEmpty(s.spawnId)) return;

        _persist.MarkKilled(s.spawnId);
        //Debug.Log($"[PERSIST] MarkKilled: {s.spawnId}");

    }
}