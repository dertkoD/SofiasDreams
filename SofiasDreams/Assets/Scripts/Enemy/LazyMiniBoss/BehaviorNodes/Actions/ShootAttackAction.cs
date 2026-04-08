using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Shoot Attack",
    story: "[Self] performs shoot attack",
    category: "Action/LazyMiniBoss",
    id: "a1b2c3d4e5f60001000000000000aa07")]
public partial class ShootAttackAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;

    protected override Status OnStart()
    {
        if (Self.Value == null) return Status.Failure;
        var bridge = Self.Value.GetComponent<LazyMiniBossGraphBridge>();
        if (bridge == null) return Status.Failure;

        bridge.Motor.Stop();
        bridge.Anim.TriggerShoot();
        bridge.NextShootAttackTime = Time.time + bridge.Config.shootAttackCooldown;
        bridge.LastRangedWasShoot = true;
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        var bridge = Self.Value.GetComponent<LazyMiniBossGraphBridge>();
        if (bridge == null) return Status.Failure;

        if (bridge.Anim.IsInAgroMovement())
            return Status.Success;

        return Status.Running;
    }
}
