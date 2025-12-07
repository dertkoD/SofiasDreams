using System;
using Unity.Cinemachine;
using UnityEngine;
using Zenject;

public class CameraTargetBinder : MonoBehaviour, IInitializable, IDisposable
{
    [Inject] SignalBus _bus;
    [SerializeField] CinemachineCamera _cam;

    void Reset() { _cam = GetComponent<CinemachineCamera>(); }

    public void Initialize()
    {
        _bus.Subscribe<PlayerSpawned>(OnSpawned);
    }

    public void Dispose()
    { 
        _bus.TryUnsubscribe<PlayerSpawned>(OnSpawned);
    }

    void OnSpawned(PlayerSpawned s)
    {
        var t = s.facade.cameraTarget;
        if (_cam == null) _cam = GetComponent<CinemachineCamera>();
        _cam.Follow = t;
        // _cam.LookAt = t; // если нужно
    }
}
