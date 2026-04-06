using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Bull Shoot",
    story: "[Agent] shoots projectile",
    category: "Action/BullEnemy",
    id: "b0e1a001-0005-4000-8000-000000000005")]
public partial class BullShootAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;

    protected override Status OnStart()
    {
        if (Agent?.Value == null) return Status.Failure;

        var bridge = Agent.Value.GetComponent<BullBehaviorBridge>();
        if (bridge == null) return Status.Failure;

        bridge.Motor.Stop();
        bridge.Anim.TriggerShoot();
        bridge.NextShootAttackTime = Time.time + bridge.Config.shootAttackCooldown;
        bridge.UseAttack3Next = true;
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Agent?.Value == null) return Status.Failure;

        var bridge = Agent.Value.GetComponent<BullBehaviorBridge>();
        if (bridge == null) return Status.Failure;

        if (bridge.Anim.IsInAgroMovement())
            return Status.Success;

        return Status.Running;
    }
}
