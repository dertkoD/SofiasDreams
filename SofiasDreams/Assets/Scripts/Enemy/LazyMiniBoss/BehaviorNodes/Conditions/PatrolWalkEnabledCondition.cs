using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Patrol Walk Enabled",
    story: "[Self] patrol walk enabled",
    category: "Condition/LazyMiniBoss",
    id: "a1b2c3d4e5f60001000000000000bb10")]
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
