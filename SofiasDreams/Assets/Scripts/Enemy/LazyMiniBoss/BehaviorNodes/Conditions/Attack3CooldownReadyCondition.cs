using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "Attack3 Cooldown Ready", story: "[Self] attack3 cooldown ready", category: "Conditions", id: "8d8183a67ff156a87ae1e347e0198287")]
public partial class Attack3CooldownReadyCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;

    public override bool IsTrue()
    {
        if (Self.Value == null) return false;
        var bridge = Self.Value.GetComponent<LazyMiniBossGraphBridge>();
        if (bridge == null) return false;
        return Time.time >= bridge.NextAttack3Time;
    }
}
