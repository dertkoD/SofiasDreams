using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Bull Trigger Patrol",
    story: "[Agent] returns to sleep",
    category: "Action/BullEnemy",
    id: "b0e1a001-0007-4000-8000-000000000007")]
public partial class BullTriggerPatrolAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;

    protected override Status OnStart()
    {
        if (Agent?.Value == null) return Status.Failure;

        var bridge = Agent.Value.GetComponent<BullBehaviorBridge>();
        if (bridge == null) return Status.Failure;

        bridge.Motor.Stop();
        bridge.Anim.TriggerPatrol();
        bridge.HasSeenPlayer = false;
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Agent?.Value == null) return Status.Failure;

        var bridge = Agent.Value.GetComponent<BullBehaviorBridge>();
        if (bridge == null) return Status.Failure;

        if (bridge.Anim.IsInSleep())
            return Status.Success;

        return Status.Running;
    }
}
