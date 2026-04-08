using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Agro Chase",
    story: "[Self] chases player in agro",
    category: "Action/LazyMiniBoss",
    id: "a1b2c3d4e5f60001000000000000aa05")]
public partial class AgroChaseAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;

    protected override Status OnStart()
    {
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Self.Value == null) return Status.Failure;
        var bridge = Self.Value.GetComponent<LazyMiniBossGraphBridge>();
        if (bridge == null) return Status.Failure;

        bridge.FacePlayer();
        bridge.ClampToZone();

        Vector3 rawTargetPos = bridge.SeesPlayer() && bridge.Player != null
            ? bridge.Player.position
            : (Vector3)bridge.LastSeenPos;

        Vector3 moveTarget = rawTargetPos;
        if (bridge.ZoneReady)
            moveTarget.x = Mathf.Clamp(rawTargetPos.x, bridge.ZoneMinX, bridge.ZoneMaxX);

        float distToMove = Vector2.Distance(bridge.transform.position, moveTarget);
        float dxToMove = moveTarget.x - bridge.transform.position.x;

        if (distToMove > bridge.Config.closeRangeThreshold * 0.8f)
        {
            if (Mathf.Abs(dxToMove) > 0.05f)
                bridge.Motor.Move(Mathf.Sign(dxToMove) * bridge.Config.agroRunSpeed);
            else
                bridge.Motor.Stop();
        }
        else
        {
            bridge.Motor.Stop();
        }

        return Status.Running;
    }

    protected override void OnEnd()
    {
        if (Self.Value == null) return;
        var bridge = Self.Value.GetComponent<LazyMiniBossGraphBridge>();
        if (bridge != null) bridge.Motor.Stop();
    }
}
