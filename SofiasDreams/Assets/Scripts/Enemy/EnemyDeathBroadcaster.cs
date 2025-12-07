using UnityEngine;
using Zenject;

public class EnemyDeathBroadcaster : MonoBehaviour
{
    SignalBus _bus;
    [Inject] void Inject(SignalBus bus) => _bus = bus;

    public void OnEnemyDied()
    {
        _bus.Fire(new EnemyKilled());
    }
}
