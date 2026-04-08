using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "Forget Timer Expired", story: "[Self] forget timer expired", category: "Conditions", id: "9c2e2966abc87c073b24d6e296963991")]
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
