using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Bull Sleep",
    story: "[Agent] sleeps in place",
    category: "Action/BullEnemy",
    id: "b0e1a001-0001-4000-8000-000000000001")]
public partial class BullSleepAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;

    protected override Status OnStart()
    {
        if (Agent?.Value == null) return Status.Failure;

        var bridge = Agent.Value.GetComponent<BullBehaviorBridge>();
        if (bridge == null) return Status.Failure;

        bridge.Motor.Stop();
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Agent?.Value == null) return Status.Failure;

        var bridge = Agent.Value.GetComponent<BullBehaviorBridge>();
        if (bridge == null) return Status.Failure;

        if (bridge.TrySense(out Transform target))
        {
            bridge.Player = target;
            bridge.LastSeenPos = target.position;
            bridge.HasSeenPlayer = true;
            return Status.Success;
        }

        if (bridge.Config.canWalkInPatrol)
        {
            var brain = bridge.Brain;
            if (brain != null && brain.CurrentPath != null && brain.CurrentPath.Count > 0)
            {
                Vector3 wp = brain.CurrentPath.GetPoint(brain.PathIndex);
                float dist = Vector2.Distance(bridge.transform.position, wp);

                if (dist <= bridge.Config.waypointArriveDistance)
                {
                    brain.AdvancePathIndex();
                    bridge.Motor.Stop();
                    return Status.Running;
                }

                float dx = wp.x - bridge.transform.position.x;
                if (Mathf.Abs(dx) < 0.1f)
                {
                    brain.AdvancePathIndex();
                    bridge.Motor.Stop();
                    return Status.Running;
                }

                bridge.Motor.Move(Mathf.Sign(dx) * bridge.Config.patrolSpeed);
            }
        }

        return Status.Running;
    }
}
