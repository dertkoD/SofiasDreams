using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Is Alive",
    story: "[Self] is alive",
    category: "Condition/LazyMiniBoss",
    id: "a1b2c3d4e5f60001000000000000bb09")]
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
