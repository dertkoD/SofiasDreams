using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

/// <summary>
/// Full agro combat loop as a single Behavior Graph action.
///
/// Cycle:
///   1. If player in melee range → melee (Attack1+Attack2)
///   2. Else → Shoot, then check melee again, then Attack3
///   3. After one ranged cycle (Shoot + Attack3) → walk a few steps toward player
///   4. Go to 1.
///
/// Every frame checks forget timer. Returns Failure when timer expires → exits agro.
/// Stays within EnemyPatrolPath zone at all times.
/// </summary>
[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Agro Combat Loop",
    story: "[Self] runs full agro combat loop",
    category: "Action/LazyMiniBoss",
    id: "a1b2c3d4e5f60001000000000000aa20")]
public partial class AgroCombatLoopAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;

    enum Phase
    {
        Decide,
        MeleeAttack1,
        MeleeAttack2,
        MeleeWaitEnd,
        Shoot,
        PreAttack3MeleeCheck,
        Attack3,
        Approach,
    }

    Phase _phase;
    float _approachUntil;

    protected override Status OnStart()
    {
        if (Self.Value == null) return Status.Failure;
        var b = Self.Value.GetComponent<LazyMiniBossGraphBridge>();
        if (b == null) return Status.Failure;

        _phase = Phase.Decide;
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        var b = Self.Value.GetComponent<LazyMiniBossGraphBridge>();
        if (b == null) return Status.Failure;

        if (b.ForgetTimer <= 0 && !b.SeesPlayer())
            return Status.Success;

        b.FacePlayer();
        b.ClampToZone();

        switch (_phase)
        {
            case Phase.Decide:
                return TickDecide(b);
            case Phase.MeleeAttack1:
                return TickMeleeAttack1(b);
            case Phase.MeleeAttack2:
                return TickMeleeAttack2(b);
            case Phase.MeleeWaitEnd:
                return TickMeleeWaitEnd(b);
            case Phase.Shoot:
                return TickShoot(b);
            case Phase.PreAttack3MeleeCheck:
                return TickPreAttack3MeleeCheck(b);
            case Phase.Attack3:
                return TickAttack3(b);
            case Phase.Approach:
                return TickApproach(b);
        }

        return Status.Running;
    }

    protected override void OnEnd()
    {
        if (Self.Value == null) return;
        var b = Self.Value.GetComponent<LazyMiniBossGraphBridge>();
        if (b == null) return;
        b.Motor.Stop();
        b.Anim.SetAttack1(false);
        b.Anim.SetAttack2(false);
        b.Anim.SetAttack3(false);
    }

    // ------------------------------------------------------------------

    Status TickDecide(LazyMiniBossGraphBridge b)
    {
        b.Motor.Stop();

        float dist = b.DistanceToPlayer();

        if (dist <= b.Config.closeRangeThreshold)
        {
            StartMelee(b);
            return Status.Running;
        }

        StartShoot(b);
        return Status.Running;
    }

    // --- Melee ---

    void StartMelee(LazyMiniBossGraphBridge b)
    {
        b.Motor.Stop();
        b.Anim.SetAttack1(true);
        _phase = Phase.MeleeAttack1;
    }

    Status TickMeleeAttack1(LazyMiniBossGraphBridge b)
    {
        if (b.Anim.IsInAttack1())
        {
            b.Anim.SetAttack2(true);
            _phase = Phase.MeleeAttack2;
        }
        return Status.Running;
    }

    Status TickMeleeAttack2(LazyMiniBossGraphBridge b)
    {
        if (b.Anim.IsInAttack2())
        {
            b.Anim.SetAttack1(false);
            b.Anim.SetAttack2(false);
            _phase = Phase.MeleeWaitEnd;
        }
        return Status.Running;
    }

    Status TickMeleeWaitEnd(LazyMiniBossGraphBridge b)
    {
        if (b.Anim.IsInAgroMovement())
        {
            _phase = Phase.Decide;
        }
        return Status.Running;
    }

    // --- Shoot ---

    void StartShoot(LazyMiniBossGraphBridge b)
    {
        b.Motor.Stop();
        b.Anim.TriggerShoot();
        _phase = Phase.Shoot;
    }

    Status TickShoot(LazyMiniBossGraphBridge b)
    {
        if (b.Anim.IsInAgroMovement())
        {
            _phase = Phase.PreAttack3MeleeCheck;
        }
        return Status.Running;
    }

    // --- Pre-Attack3 melee check ---

    Status TickPreAttack3MeleeCheck(LazyMiniBossGraphBridge b)
    {
        float dist = b.DistanceToPlayer();
        if (dist <= b.Config.closeRangeThreshold)
        {
            StartMelee(b);
            return Status.Running;
        }

        b.Motor.Stop();
        b.Anim.SetAttack3(true);
        _phase = Phase.Attack3;
        return Status.Running;
    }

    // --- Attack3 ---

    Status TickAttack3(LazyMiniBossGraphBridge b)
    {
        if (b.Anim.IsInAgroMovement())
        {
            b.Anim.SetAttack3(false);
            StartApproach(b);
        }
        return Status.Running;
    }

    // --- Approach ---

    void StartApproach(LazyMiniBossGraphBridge b)
    {
        _approachUntil = Time.time + b.Config.agroApproachDuration;
        _phase = Phase.Approach;
    }

    Status TickApproach(LazyMiniBossGraphBridge b)
    {
        if (Time.time >= _approachUntil)
        {
            b.Motor.Stop();
            _phase = Phase.Decide;
            return Status.Running;
        }

        if (b.Player == null)
        {
            b.Motor.Stop();
            _phase = Phase.Decide;
            return Status.Running;
        }

        float dist = b.DistanceToPlayer();
        if (dist <= b.Config.closeRangeThreshold)
        {
            b.Motor.Stop();
            _phase = Phase.Decide;
            return Status.Running;
        }

        Vector3 target = b.Player.position;
        if (b.ZoneReady)
            target.x = Mathf.Clamp(target.x, b.ZoneMinX, b.ZoneMaxX);

        float dx = target.x - b.transform.position.x;
        if (Mathf.Abs(dx) > 0.05f)
            b.Motor.Move(Mathf.Sign(dx) * b.Config.agroRunSpeed);
        else
            b.Motor.Stop();

        return Status.Running;
    }
}
