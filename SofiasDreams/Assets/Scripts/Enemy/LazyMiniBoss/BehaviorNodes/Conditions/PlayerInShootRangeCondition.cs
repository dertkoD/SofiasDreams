using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "Player In Shoot Range", story: "[Self] player in shoot range", category: "Conditions", id: "82bf2bce142e0904d1186784fbbee0cf")]
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
