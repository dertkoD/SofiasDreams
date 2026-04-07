using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Bull Combat",
    story: "[Agent] fights the player (chase + melee + shoot/attack3)",
    category: "Action/BullEnemy",
    id: "b0e1a001-0010-4000-8000-000000000010")]
public partial class BullCombatAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;

    protected override Status OnStart()
    {
        if (Agent?.Value == null) return Status.Failure;

        var bridge = Agent.Value.GetComponent<BullBehaviorBridge>();
        if (bridge == null) return Status.Failure;

        bridge.ForgetTimer = bridge.Config.agroForgetSeconds;
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Agent?.Value == null) return Status.Failure;

        var bridge = Agent.Value.GetComponent<BullBehaviorBridge>();
        if (bridge == null) return Status.Failure;

        if (bridge.HealthComponent != null && bridge.HealthComponent.CurrentHP <= 0)
        {
            bridge.Motor.Stop();
            bridge.Anim.TriggerDeath();
            return Status.Failure;
        }

        bool seesPlayer = bridge.TrySense(out Transform t);
        if (seesPlayer)
        {
            bridge.Player = t;
            bridge.LastSeenPos = t.position;
            bridge.HasSeenPlayer = true;
            bridge.ForgetTimer = bridge.Config.agroForgetSeconds;
        }
        else
        {
            bridge.ForgetTimer -= Time.deltaTime;
        }

        if (bridge.ForgetTimer <= 0 && !seesPlayer)
            return Status.Success;

        if (bridge.Anim.IsInAttack1() || bridge.Anim.IsInAttack2() ||
            bridge.Anim.IsInAttack3() || bridge.Anim.IsInShoot())
        {
            return Status.Running;
        }

        var config = bridge.Config;
        Vector3 targetPos = seesPlayer && bridge.Player != null
            ? bridge.Player.position
            : (Vector3)bridge.LastSeenPos;
        float dist = Vector2.Distance(bridge.transform.position, targetPos);
        float dx = targetPos.x - bridge.transform.position.x;

        if (Mathf.Abs(dx) > 0.1f)
            bridge.Motor.Face(dx > 0 ? 1 : -1);

        if (bridge.ZoneReady)
        {
            float myX = bridge.transform.position.x;
            if (myX < bridge.ZoneMinX) { bridge.Motor.Move(config.agroRunSpeed); return Status.Running; }
            if (myX > bridge.ZoneMaxX) { bridge.Motor.Move(-config.agroRunSpeed); return Status.Running; }
        }

        if (dist <= config.closeRangeThreshold && Time.time >= bridge.NextMeleeAttackTime)
        {
            bridge.Motor.Stop();
            bridge.Anim.SetAttack1(true);
            bridge.NextMeleeAttackTime = Time.time + config.meleeAttackCooldown;
            return Status.Running;
        }

        if (seesPlayer && dist >= config.shootRangeMin && Time.time >= bridge.NextShootAttackTime)
        {
            bridge.Motor.Stop();
            if (bridge.UseAttack3Next)
            {
                bridge.Anim.SetAttack3(true);
                bridge.UseAttack3Next = false;
            }
            else
            {
                bridge.Anim.TriggerShoot();
                bridge.UseAttack3Next = true;
            }
            bridge.NextShootAttackTime = Time.time + config.shootAttackCooldown;
            return Status.Running;
        }

        Vector3 clampedTarget = targetPos;
        if (bridge.ZoneReady)
            clampedTarget.x = Mathf.Clamp(targetPos.x, bridge.ZoneMinX, bridge.ZoneMaxX);

        float dxClamped = clampedTarget.x - bridge.transform.position.x;
        float distClamped = Vector2.Distance(bridge.transform.position, clampedTarget);

        if (distClamped > config.closeRangeThreshold * 0.8f)
        {
            if (seesPlayer && dist >= config.shootRangeMin &&
                dist <= config.shootRangeMin + 2f &&
                Time.time < bridge.NextShootAttackTime)
            {
                bridge.Motor.Stop();
            }
            else if (Mathf.Abs(dxClamped) > 0.05f)
            {
                bridge.Motor.Move(Mathf.Sign(dxClamped) * config.agroRunSpeed);
            }
            else
            {
                bridge.Motor.Stop();
            }
        }
        else
        {
            bridge.Motor.Stop();
        }

        return Status.Running;
    }

    protected override void OnEnd()
    {
        if (Agent?.Value == null) return;
        var bridge = Agent.Value.GetComponent<BullBehaviorBridge>();
        if (bridge == null) return;
        bridge.Motor.Stop();
        bridge.Anim.SetAttack1(false);
        bridge.Anim.SetAttack2(false);
        bridge.Anim.SetAttack3(false);
    }
}
