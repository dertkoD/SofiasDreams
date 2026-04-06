using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

[Serializable, GeneratePropertyBag]
[Condition(
    name: "Bull Player In Shoot Range",
    story: "player is in shoot range of [Agent]",
    category: "Conditions/BullEnemy",
    id: "b0e1c001-0004-4000-8000-000000000004")]
public partial class BullPlayerInShootRangeCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;

    public override bool IsTrue()
    {
        if (Agent?.Value == null) return false;

        var bridge = Agent.Value.GetComponent<BullBehaviorBridge>();
        if (bridge == null) return false;

        return bridge.IsPlayerInShootRange();
    }
}
