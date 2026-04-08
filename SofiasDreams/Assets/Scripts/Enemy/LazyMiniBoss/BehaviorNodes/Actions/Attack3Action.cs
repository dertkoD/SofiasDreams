using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Attack3",
    story: "[Self] performs attack3",
    category: "Action/LazyMiniBoss",
    id: "a1b2c3d4e5f60001000000000000aa08")]
public partial class Attack3Action : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;

    protected override Status OnStart()
    {
        if (Self.Value == null) return Status.Failure;
        var bridge = Self.Value.GetComponent<LazyMiniBossGraphBridge>();
        if (bridge == null) return Status.Failure;

        bridge.Motor.Stop();
        bridge.Anim.SetAttack3(true);
        bridge.NextAttack3Time = Time.time + bridge.Config.attack3Cooldown;
        bridge.LastRangedWasShoot = false;
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        var bridge = Self.Value.GetComponent<LazyMiniBossGraphBridge>();
        if (bridge == null) return Status.Failure;

        if (bridge.Anim.IsInAgroMovement())
        {
            bridge.Anim.SetAttack3(false);
            return Status.Success;
        }

        return Status.Running;
    }

    protected override void OnEnd()
    {
        if (Self.Value == null) return;
        var bridge = Self.Value.GetComponent<LazyMiniBossGraphBridge>();
        if (bridge != null) bridge.Anim.SetAttack3(false);
    }
}
