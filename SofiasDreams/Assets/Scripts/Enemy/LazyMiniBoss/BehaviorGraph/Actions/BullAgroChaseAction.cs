using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Bull Agro Chase",
    story: "[Agent] chases player in agro",
    category: "Action/BullEnemy",
    id: "b0e1a001-0003-4000-8000-000000000003")]
public partial class BullAgroChaseAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;

    protected override Status OnStart()
    {
        if (Agent?.Value == null) return Status.Failure;
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Agent?.Value == null) return Status.Failure;

        var bridge = Agent.Value.GetComponent<BullBehaviorBridge>();
        if (bridge == null) return Status.Failure;
        var config = bridge.Config;

        bool seesPlayer = bridge.TrySense(out Transform t);
        if (seesPlayer)
        {
            bridge.Player = t;
            bridge.LastSeenPos = t.position;
            bridge.HasSeenPlayer = true;
            bridge.ForgetTimer = config.agroForgetSeconds;
        }
        else
        {
            bridge.ForgetTimer -= Time.deltaTime;
        }

        if (bridge.ForgetTimer <= 0 && !seesPlayer)
            return Status.Failure;

        Vector3 rawTargetPos = seesPlayer && bridge.Player != null
            ? bridge.Player.position
            : (Vector3)bridge.LastSeenPos;

        float distToPlayer = Vector2.Distance(bridge.transform.position, rawTargetPos);
        float dxToPlayer = rawTargetPos.x - bridge.transform.position.x;

        if (Mathf.Abs(dxToPlayer) > 0.1f)
            bridge.Motor.Face(dxToPlayer > 0 ? 1 : -1);

        if (bridge.ZoneReady)
        {
            float myX = bridge.transform.position.x;
            if (myX < bridge.ZoneMinX) { bridge.Motor.Move(config.agroRunSpeed); return Status.Running; }
            if (myX > bridge.ZoneMaxX) { bridge.Motor.Move(-config.agroRunSpeed); return Status.Running; }
        }

        if (distToPlayer <= config.closeRangeThreshold)
        {
            bridge.Motor.Stop();
            return Status.Running;
        }

        Vector3 clampedTarget = rawTargetPos;
        if (bridge.ZoneReady)
            clampedTarget.x = Mathf.Clamp(rawTargetPos.x, bridge.ZoneMinX, bridge.ZoneMaxX);

        float dxClamped = clampedTarget.x - bridge.transform.position.x;

        if (seesPlayer && distToPlayer >= config.shootRangeMin &&
            distToPlayer <= config.shootRangeMin + 2f &&
            Time.time < bridge.NextShootAttackTime)
        {
            bridge.Motor.Stop();
            return Status.Running;
        }

        if (Mathf.Abs(dxClamped) > 0.05f)
            bridge.Motor.Move(Mathf.Sign(dxClamped) * config.agroRunSpeed);
        else
            bridge.Motor.Stop();

        return Status.Running;
    }

    protected override void OnEnd()
    {
        if (Agent?.Value == null) return;
        var bridge = Agent.Value.GetComponent<BullBehaviorBridge>();
        bridge?.Motor.Stop();
    }
}
