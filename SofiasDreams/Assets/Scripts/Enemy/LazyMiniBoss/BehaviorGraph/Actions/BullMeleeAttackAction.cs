using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Bull Melee Attack",
    story: "[Agent] performs melee combo (Attack1 + Attack2)",
    category: "Action/BullEnemy",
    id: "b0e1a001-0004-4000-8000-000000000004")]
public partial class BullMeleeAttackAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;

    bool _attack2Triggered;

    protected override Status OnStart()
    {
        if (Agent?.Value == null) return Status.Failure;

        var bridge = Agent.Value.GetComponent<BullBehaviorBridge>();
        if (bridge == null) return Status.Failure;

        bridge.Motor.Stop();
        _attack2Triggered = false;
        bridge.Anim.SetAttack1(true);
        bridge.NextMeleeAttackTime = Time.time + bridge.Config.meleeAttackCooldown;
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Agent?.Value == null) return Status.Failure;

        var bridge = Agent.Value.GetComponent<BullBehaviorBridge>();
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
        if (Agent?.Value == null) return;
        var bridge = Agent.Value.GetComponent<BullBehaviorBridge>();
        if (bridge == null) return;
        bridge.Anim.SetAttack1(false);
        bridge.Anim.SetAttack2(false);
    }
}
