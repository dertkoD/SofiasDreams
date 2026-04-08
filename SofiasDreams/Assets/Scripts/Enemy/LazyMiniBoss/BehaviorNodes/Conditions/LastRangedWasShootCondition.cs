using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "Last Ranged Was Shoot", story: "[Self] last ranged was shoot", category: "Conditions", id: "d6130c9b3c740ad9c1760f7087030f35")]
public partial class LastRangedWasShootCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;

    public override bool IsTrue()
    {
        if (Self.Value == null) return false;
        var bridge = Self.Value.GetComponent<LazyMiniBossGraphBridge>();
        if (bridge == null) return false;
        return bridge.LastRangedWasShoot;
    }
}
