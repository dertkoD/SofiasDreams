using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

[Serializable, GeneratePropertyBag]
[Condition(
    name: "Bull Is Alive",
    story: "[Agent] is alive",
    category: "Conditions/BullEnemy",
    id: "b0e1c001-0002-4000-8000-000000000002")]
public partial class BullIsAliveCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;

    public override bool IsTrue()
    {
        if (Agent?.Value == null) return false;

        var bridge = Agent.Value.GetComponent<BullBehaviorBridge>();
        if (bridge == null || bridge.HealthComponent == null) return false;

        return bridge.HealthComponent.CurrentHP > 0;
    }
}
