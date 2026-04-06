using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Bull Trigger Agro",
    story: "[Agent] triggers agro animation",
    category: "Action/BullEnemy",
    id: "b0e1a001-0002-4000-8000-000000000002")]
public partial class BullTriggerAgroAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;

    protected override Status OnStart()
    {
        if (Agent?.Value == null) return Status.Failure;

        var bridge = Agent.Value.GetComponent<BullBehaviorBridge>();
        if (bridge == null) return Status.Failure;

        bridge.Motor.Stop();
        bridge.Anim.TriggerAgro();
        bridge.ForgetTimer = bridge.Config.agroForgetSeconds;
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Agent?.Value == null) return Status.Failure;

        var bridge = Agent.Value.GetComponent<BullBehaviorBridge>();
        if (bridge == null) return Status.Failure;

        if (bridge.Anim.IsInAgroMovement())
            return Status.Success;

        return Status.Running;
    }
}
