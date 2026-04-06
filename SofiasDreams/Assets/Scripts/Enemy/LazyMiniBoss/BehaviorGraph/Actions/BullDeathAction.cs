using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Bull Death",
    story: "[Agent] dies",
    category: "Action/BullEnemy",
    id: "b0e1a001-0008-4000-8000-000000000008")]
public partial class BullDeathAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;

    protected override Status OnStart()
    {
        if (Agent?.Value == null) return Status.Failure;

        var bridge = Agent.Value.GetComponent<BullBehaviorBridge>();
        if (bridge == null) return Status.Failure;

        bridge.Motor.Stop();
        bridge.Anim.TriggerDeath();
        return Status.Success;
    }
}
