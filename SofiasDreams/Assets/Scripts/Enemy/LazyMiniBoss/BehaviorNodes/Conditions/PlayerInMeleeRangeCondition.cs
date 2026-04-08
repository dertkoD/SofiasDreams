using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "Player In Melee Range", story: "[Self] player in melee range", category: "Conditions", id: "2c85e318712541fac6fd6f809f9e9c8a")]
public partial class PlayerInMeleeRangeCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;

    public override bool IsTrue()
    {
        if (Self.Value == null) return false;
        var bridge = Self.Value.GetComponent<LazyMiniBossGraphBridge>();
        if (bridge == null || bridge.Player == null) return false;
        return bridge.DistanceToPlayer() <= bridge.Config.closeRangeThreshold;
    }
}