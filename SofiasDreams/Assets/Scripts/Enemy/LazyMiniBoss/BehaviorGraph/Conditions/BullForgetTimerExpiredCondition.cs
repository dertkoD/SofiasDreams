using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

[Serializable, GeneratePropertyBag]
[Condition(
    name: "Bull Forget Timer Expired",
    story: "[Agent] has forgotten the player",
    category: "Conditions/BullEnemy",
    id: "b0e1c001-0008-4000-8000-000000000008")]
public partial class BullForgetTimerExpiredCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;

    public override bool IsTrue()
    {
        if (Agent?.Value == null) return false;

        var bridge = Agent.Value.GetComponent<BullBehaviorBridge>();
        if (bridge == null) return false;

        return bridge.ForgetTimer <= 0f && !bridge.TrySense(out _);
    }
}
