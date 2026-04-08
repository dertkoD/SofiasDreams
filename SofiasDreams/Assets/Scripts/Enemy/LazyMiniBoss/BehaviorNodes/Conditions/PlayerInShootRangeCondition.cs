using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Player In Shoot Range",
    story: "[Self] player in shoot range",
    category: "Condition/LazyMiniBoss",
    id: "a1b2c3d4e5f60001000000000000bb03")]
public partial class PlayerInShootRangeCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;

    public override bool IsTrue()
    {
        if (Self.Value == null) return false;
        var bridge = Self.Value.GetComponent<LazyMiniBossGraphBridge>();
        if (bridge == null || bridge.Player == null) return false;
        return bridge.SeesPlayer() && bridge.DistanceToPlayer() >= bridge.Config.shootRangeMin;
    }
}
