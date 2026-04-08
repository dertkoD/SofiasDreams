using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Last Ranged Was Shoot",
    story: "[Self] last ranged was shoot",
    category: "Condition/LazyMiniBoss",
    id: "a1b2c3d4e5f60001000000000000bb08")]
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
