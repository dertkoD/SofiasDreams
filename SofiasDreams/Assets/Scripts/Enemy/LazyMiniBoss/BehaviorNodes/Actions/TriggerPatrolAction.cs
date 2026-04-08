using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Trigger Patrol",
    story: "[Self] triggers patrol animation",
    category: "Action/LazyMiniBoss",
    id: "a1b2c3d4e5f60001000000000000aa04")]
public partial class TriggerPatrolAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;

    protected override Status OnStart()
    {
        if (Self.Value == null) return Status.Failure;
        var bridge = Self.Value.GetComponent<LazyMiniBossGraphBridge>();
        if (bridge == null) return Status.Failure;

        bridge.Motor.Stop();
        bridge.Anim.TriggerPatrol();
        bridge.HasSeenPlayer = false;
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        var bridge = Self.Value.GetComponent<LazyMiniBossGraphBridge>();
        if (bridge == null) return Status.Failure;

        bool arrived = bridge.Config.patrolWalk
            ? bridge.Anim.IsInPatrolMovement()
            : bridge.Anim.IsInSleep() || bridge.Anim.IsInPatrolMovement();

        if (arrived)
            return Status.Success;

        return Status.Running;
    }
}
