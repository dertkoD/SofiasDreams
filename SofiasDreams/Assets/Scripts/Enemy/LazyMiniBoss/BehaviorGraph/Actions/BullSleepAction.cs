using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Bull Sleep",
    story: "[Agent] sleeps in place",
    category: "Action/BullEnemy",
    id: "b0e1a001-0001-4000-8000-000000000001")]
public partial class BullSleepAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;

    protected override Status OnStart()
    {
        if (Agent?.Value == null) return Status.Failure;

        var bridge = Agent.Value.GetComponent<BullBehaviorBridge>();
        if (bridge == null) return Status.Failure;

        bridge.Motor.Stop();
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Agent?.Value == null) return Status.Failure;

        var bridge = Agent.Value.GetComponent<BullBehaviorBridge>();
        if (bridge == null) return Status.Failure;

        if (bridge.TrySense(out Transform target))
        {
            bridge.Player = target;
            bridge.LastSeenPos = target.position;
            bridge.HasSeenPlayer = true;
            return Status.Success;
        }

        return Status.Running;
    }
}
