using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "Patrol Walk Enabled", story: "[Self] patrol walk enabled", category: "Conditions", id: "95e0062c38e717d800215065aa784d2b")]
public partial class PatrolWalkEnabledCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;

    public override bool IsTrue()
    {
        if (Self.Value == null) return false;
        var bridge = Self.Value.GetComponent<LazyMiniBossGraphBridge>();
        if (bridge == null || bridge.Config == null) return false;
        return bridge.Config.patrolWalk;
    }
}
