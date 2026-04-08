using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Melee Cooldown Ready",
    story: "[Self] melee cooldown ready",
    category: "Condition/LazyMiniBoss",
    id: "a1b2c3d4e5f60001000000000000bb04")]
public partial class MeleeCooldownReadyCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;

    public override bool IsTrue()
    {
        if (Self.Value == null) return false;
        var bridge = Self.Value.GetComponent<LazyMiniBossGraphBridge>();
        if (bridge == null) return false;
        return Time.time >= bridge.NextMeleeAttackTime;
    }
}
