using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

[Serializable, GeneratePropertyBag]
[Condition(
    name: "Bull Should Use Attack3",
    story: "[Agent] should use attack 3 next",
    category: "Conditions/BullEnemy",
    id: "b0e1c001-0005-4000-8000-000000000005")]
public partial class BullShouldUseAttack3Condition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;

    public override bool IsTrue()
    {
        if (Agent?.Value == null) return false;

        var bridge = Agent.Value.GetComponent<BullBehaviorBridge>();
        if (bridge == null) return false;

        return bridge.UseAttack3Next;
    }
}
