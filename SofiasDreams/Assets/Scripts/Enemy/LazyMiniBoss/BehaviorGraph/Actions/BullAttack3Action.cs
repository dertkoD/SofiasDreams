using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Bull Attack3",
    story: "[Agent] performs ranged attack 3",
    category: "Action/BullEnemy",
    id: "b0e1a001-0006-4000-8000-000000000006")]
public partial class BullAttack3Action : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;

    protected override Status OnStart()
    {
        if (Agent?.Value == null) return Status.Failure;

        var bridge = Agent.Value.GetComponent<BullBehaviorBridge>();
        if (bridge == null) return Status.Failure;

        bridge.Motor.Stop();
        bridge.Anim.SetAttack3(true);
        bridge.NextShootAttackTime = Time.time + bridge.Config.shootAttackCooldown;
        bridge.UseAttack3Next = false;
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Agent?.Value == null) return Status.Failure;

        var bridge = Agent.Value.GetComponent<BullBehaviorBridge>();
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
        if (Agent?.Value == null) return;
        var bridge = Agent.Value.GetComponent<BullBehaviorBridge>();
        bridge?.Anim.SetAttack3(false);
    }
}
