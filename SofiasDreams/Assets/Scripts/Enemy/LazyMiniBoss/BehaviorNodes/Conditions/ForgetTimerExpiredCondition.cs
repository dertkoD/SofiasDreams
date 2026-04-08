using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Forget Timer Expired",
    story: "[Self] forget timer expired",
    category: "Condition/LazyMiniBoss",
    id: "a1b2c3d4e5f60001000000000000bb07")]
public partial class ForgetTimerExpiredCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;

    public override bool IsTrue()
    {
        if (Self.Value == null) return false;
        var bridge = Self.Value.GetComponent<LazyMiniBossGraphBridge>();
        if (bridge == null) return false;
        return bridge.ForgetTimer <= 0 && !bridge.SeesPlayer();
    }
}
