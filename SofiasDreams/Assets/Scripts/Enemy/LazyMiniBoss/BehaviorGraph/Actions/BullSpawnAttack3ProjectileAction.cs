using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(
    name: "Bull Spawn Attack3 Projectile",
    story: "[Agent] spawns attack3 projectile from MuzzleHorns",
    category: "Action/BullEnemy",
    id: "b0e1a001-0011-4000-8000-000000000011")]
public partial class BullSpawnAttack3ProjectileAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;

    protected override Status OnStart()
    {
        if (Agent?.Value == null) return Status.Failure;

        var bridge = Agent.Value.GetComponent<BullBehaviorBridge>();
        if (bridge == null) return Status.Failure;

        bridge.SpawnAttack3Projectile();
        return Status.Success;
    }
}
