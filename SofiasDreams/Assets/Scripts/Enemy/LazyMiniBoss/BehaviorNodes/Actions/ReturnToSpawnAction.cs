using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

/// <summary>
/// Walks back to spawn position, then faces spawn direction.
/// Returns Success when arrived.
/// </summary>
[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Return To Spawn",
    story: "[Self] returns to spawn point",
    category: "Action/LazyMiniBoss",
    id: "a1b2c3d4e5f60001000000000000aa21")]
public partial class ReturnToSpawnAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;

    protected override Status OnStart()
    {
        if (Self.Value == null) return Status.Failure;
        var b = Self.Value.GetComponent<LazyMiniBossGraphBridge>();
        if (b == null) return Status.Failure;

        float dist = Mathf.Abs(b.transform.position.x - b.SpawnPosition.x);
        if (dist < 0.15f)
        {
            b.Motor.Stop();
            b.Motor.Face(b.SpawnFacingSign);
            return Status.Success;
        }

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        var b = Self.Value.GetComponent<LazyMiniBossGraphBridge>();
        if (b == null) return Status.Failure;

        float dx = b.SpawnPosition.x - b.transform.position.x;
        if (Mathf.Abs(dx) < 0.15f)
        {
            b.Motor.Stop();
            b.Motor.Face(b.SpawnFacingSign);
            return Status.Success;
        }

        b.Motor.Move(Mathf.Sign(dx) * b.Config.agroRunSpeed);
        return Status.Running;
    }

    protected override void OnEnd()
    {
        if (Self.Value == null) return;
        var b = Self.Value.GetComponent<LazyMiniBossGraphBridge>();
        if (b != null) b.Motor.Stop();
    }
}
