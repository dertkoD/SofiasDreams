using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "Melee Cooldown Ready", story: "[Self] melee cooldown ready", category: "Conditions", id: "9bfe6ab1a750b89e806a40cab060cb70")]
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