using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

[Serializable, GeneratePropertyBag]
[Condition(
    name: "Bull Can See Player",
    story: "[Agent] can see a player",
    category: "Conditions/BullEnemy",
    id: "b0e1c001-0001-4000-8000-000000000001")]
public partial class BullCanSeePlayerCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;

    public override bool IsTrue()
    {
        if (Agent?.Value == null) return false;

        var bridge = Agent.Value.GetComponent<BullBehaviorBridge>();
        if (bridge == null) return false;

        return bridge.TrySense(out _);
    }
}
