using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

[Serializable, GeneratePropertyBag]
[Condition(
    name: "Bull Melee Cooldown Ready",
    story: "[Agent] melee attack is off cooldown",
    category: "Conditions/BullEnemy",
    id: "b0e1c001-0006-4000-8000-000000000006")]
public partial class BullMeleeCooldownReadyCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;

    public override bool IsTrue()
    {
        if (Agent?.Value == null) return false;

        var bridge = Agent.Value.GetComponent<BullBehaviorBridge>();
        if (bridge == null) return false;

        return Time.time >= bridge.NextMeleeAttackTime;
    }
}
