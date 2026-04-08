using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "Is Alive", story: "[Self] is alive", category: "Conditions", id: "37e8bc8e49eaee4a9b99131ee43d31ed")]
public partial class IsAliveCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;

    public override bool IsTrue()
    {
        if (Self.Value == null) return false;
        var health = Self.Value.GetComponent<Health>();
        if (health == null) return true;
        var ih = health as IHealth;
        return ih != null && ih.IsAlive;
    }
}
