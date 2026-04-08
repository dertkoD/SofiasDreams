using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Patrol Walk",
    story: "[Self] walks patrol path",
    category: "Action/LazyMiniBoss",
    id: "a1b2c3d4e5f60001000000000000aa02")]
public partial class PatrolWalkAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;

    int _pathIndex;
    int _pathDir = 1;

    protected override Status OnStart()
    {
        if (Self.Value == null) return Status.Failure;
        var bridge = Self.Value.GetComponent<LazyMiniBossGraphBridge>();
        if (bridge == null) return Status.Failure;
        if (bridge.PatrolPath == null || bridge.PatrolPath.Count == 0)
            return Status.Running;

        _pathIndex = FindNearestWaypoint(bridge);
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        var bridge = Self.Value.GetComponent<LazyMiniBossGraphBridge>();
        if (bridge == null) return Status.Failure;

        bridge.ClampToZone();

        if (bridge.Health != null && !((IHealth)bridge.Health).IsAlive)
            return Status.Failure;

        if (bridge.SeesPlayer())
            return Status.Success;

        if (bridge.PatrolPath == null || bridge.PatrolPath.Count == 0)
        {
            bridge.Motor.Stop();
            return Status.Running;
        }

        Vector3 targetPos = bridge.PatrolPath.GetPoint(_pathIndex);
        float dist = Vector2.Distance(bridge.transform.position, targetPos);

        if (dist <= bridge.Config.waypointArriveDistance)
        {
            AdvanceIndex(bridge);
            bridge.Motor.Stop();
            return Status.Running;
        }

        float dx = targetPos.x - bridge.transform.position.x;
        if (Mathf.Abs(dx) < 0.1f)
        {
            AdvanceIndex(bridge);
            bridge.Motor.Stop();
            return Status.Running;
        }

        bridge.Motor.Move(Mathf.Sign(dx) * bridge.Config.patrolSpeed);
        return Status.Running;
    }

    protected override void OnEnd()
    {
        if (Self.Value == null) return;
        var bridge = Self.Value.GetComponent<LazyMiniBossGraphBridge>();
        if (bridge != null) bridge.Motor.Stop();
    }

    void AdvanceIndex(LazyMiniBossGraphBridge bridge)
    {
        if (bridge.PatrolPath.Count <= 1) return;

        if (bridge.Config.loopPath)
        {
            _pathIndex = (_pathIndex + 1) % bridge.PatrolPath.Count;
            return;
        }

        int next = _pathIndex + _pathDir;
        if (next >= bridge.PatrolPath.Count) { _pathDir = -1; next = bridge.PatrolPath.Count - 2; }
        else if (next < 0) { _pathDir = 1; next = 1; }
        _pathIndex = Mathf.Clamp(next, 0, bridge.PatrolPath.Count - 1);
    }

    int FindNearestWaypoint(LazyMiniBossGraphBridge bridge)
    {
        int best = 0;
        float bestDist = float.PositiveInfinity;
        for (int i = 0; i < bridge.PatrolPath.Count; i++)
        {
            float d = ((Vector2)bridge.PatrolPath.GetPoint(i) - (Vector2)bridge.transform.position).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = i; }
        }
        return best;
    }
}
