using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Melee Attack",
    story: "[Self] performs melee attack (Attack1 + Attack2)",
    category: "Action/LazyMiniBoss",
    id: "a1b2c3d4e5f60001000000000000aa06")]
public partial class MeleeAttackAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;

    bool _attack2Triggered;

    protected override Status OnStart()
    {
        if (Self.Value == null) return Status.Failure;
        var bridge = Self.Value.GetComponent<LazyMiniBossGraphBridge>();
        if (bridge == null) return Status.Failure;

        bridge.Motor.Stop();
        _attack2Triggered = false;
        bridge.Anim.SetAttack1(true);
        bridge.NextMeleeAttackTime = Time.time + bridge.Config.meleeAttackCooldown;
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        var bridge = Self.Value.GetComponent<LazyMiniBossGraphBridge>();
        if (bridge == null) return Status.Failure;

        if (bridge.Anim.IsInAttack1() && !_attack2Triggered)
        {
            bridge.Anim.SetAttack2(true);
            _attack2Triggered = true;
        }

        if (bridge.Anim.IsInAttack2())
        {
            bridge.Anim.SetAttack1(false);
            bridge.Anim.SetAttack2(false);
        }

        if (bridge.Anim.IsInAgroMovement() && _attack2Triggered)
        {
            bridge.Anim.SetAttack1(false);
            bridge.Anim.SetAttack2(false);
            return Status.Success;
        }

        return Status.Running;
    }

    protected override void OnEnd()
    {
        if (Self.Value == null) return;
        var bridge = Self.Value.GetComponent<LazyMiniBossGraphBridge>();
        if (bridge != null)
        {
            bridge.Anim.SetAttack1(false);
            bridge.Anim.SetAttack2(false);
        }
    }
}
