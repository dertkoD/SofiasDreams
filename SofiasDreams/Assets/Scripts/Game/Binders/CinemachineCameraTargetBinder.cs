using Unity.Cinemachine;
using UnityEngine;
using Zenject;

public class CinemachineCameraTargetBinder : MonoBehaviour
{
    [SerializeField] private CinemachineCamera _cmCamera;

    [Inject] private SignalBus _signalBus;

    private void OnEnable()
    {
        _signalBus.Subscribe<PlayerSpawned>(OnPlayerSpawned);
    }

    private void OnDisable()
    {
        _signalBus.Unsubscribe<PlayerSpawned>(OnPlayerSpawned);
    }

    private void OnPlayerSpawned(PlayerSpawned signal)
    {
        _cmCamera.Follow = signal.facade.cameraTarget;
        Debug.Log("Player spawned");
    }
}
