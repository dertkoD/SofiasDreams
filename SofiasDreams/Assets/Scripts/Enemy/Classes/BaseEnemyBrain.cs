using UnityEngine;
using Zenject;

public abstract class BaseEnemyBrain : MonoBehaviour
{
    protected IEnemyState CurrentState;
    public IEnemyState PreviousState { get; protected set; }
    protected SignalBus Bus;

    [Inject]
    public virtual void ConstructBase(SignalBus bus)
    {
        Bus = bus;
    }

    protected virtual void Update()
    {
        if (CurrentState != null)
        {
            CurrentState.Tick();
        }
    }

    public void ChangeState(IEnemyState nextState)
    {
        if (CurrentState == nextState || nextState == null) return;

        CurrentState?.Exit();
        PreviousState = CurrentState;
        CurrentState = nextState;
        CurrentState.Enter();
    }
}
