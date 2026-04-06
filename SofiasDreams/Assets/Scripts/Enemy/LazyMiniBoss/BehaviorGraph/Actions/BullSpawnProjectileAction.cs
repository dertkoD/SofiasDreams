using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Bull Spawn Projectile",
    story: "[Agent] spawns a projectile from muzzle",
    category: "Action/BullEnemy",
    id: "b0e1a001-0009-4000-8000-000000000009")]
public partial class BullSpawnProjectileAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;

    protected override Status OnStart()
    {
        if (Agent?.Value == null) return Status.Failure;

        var bridge = Agent.Value.GetComponent<BullBehaviorBridge>();
        if (bridge == null) return Status.Failure;

        bridge.SpawnProjectile();
        return Status.Success;
    }
}
