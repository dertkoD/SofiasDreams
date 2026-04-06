using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Bull Face Player",
    story: "[Agent] faces the player",
    category: "Action/BullEnemy",
    id: "b0e1a001-000a-4000-8000-00000000000a")]
public partial class BullFacePlayerAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;

    protected override Status OnStart()
    {
        if (Agent?.Value == null) return Status.Failure;

        var bridge = Agent.Value.GetComponent<BullBehaviorBridge>();
        if (bridge == null) return Status.Failure;

        bridge.FacePlayer();
        return Status.Success;
    }
}
