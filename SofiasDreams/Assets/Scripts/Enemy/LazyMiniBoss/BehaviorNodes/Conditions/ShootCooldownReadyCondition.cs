using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "Shoot Cooldown Ready", story: "[Self] shoot cooldown ready", category: "Conditions", id: "cebe5eb8b0caf6ce42f1a58809067405")]
public partial class ShootCooldownReadyCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;

    public override bool IsTrue()
    {
        if (Self.Value == null) return false;
        var bridge = Self.Value.GetComponent<LazyMiniBossGraphBridge>();
        if (bridge == null) return false;
        return Time.time >= bridge.NextShootAttackTime;
    }
}
