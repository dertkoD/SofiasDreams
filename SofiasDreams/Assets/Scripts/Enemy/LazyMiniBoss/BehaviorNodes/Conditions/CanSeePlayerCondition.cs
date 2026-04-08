using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "Can See Player", story: "[Self] can see player", category: "Conditions", id: "058b006d54b9fc040043e486294cd701")]
public partial class CanSeePlayerCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;

    public override bool IsTrue()
    {
        if (Self.Value == null) return false;
        var bridge = Self.Value.GetComponent<LazyMiniBossGraphBridge>();
        if (bridge == null) return false;
        return bridge.SeesPlayer();
    }
}