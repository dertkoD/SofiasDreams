using Zenject;

public static class PlayerSignalRegistry
{
    public static void DeclareSceneSignals(DiContainer container)
    {
        container.DeclareSignal<AttackStarted>();
        container.DeclareSignal<AttackFinished>();
        container.DeclareSignal<HealStarted>();
        container.DeclareSignal<HealFinished>();
        container.DeclareSignal<HealInterrupted>();
        container.DeclareSignal<TookDamage>();
        container.DeclareSignal<Died>();
        container.DeclareSignal<PlayerSpawned>();
        container.DeclareSignal<HealChargesChanged>();
        container.DeclareSignal<DashStarted>();
        container.DeclareSignal<DashFinished>();
        container.DeclareSignal<PlayerGrappleRequested>();
        container.DeclareSignal<GrappleCommand>();
        container.DeclareSignal<GrappleStarted>();
        container.DeclareSignal<GrappleFinished>();
        container.DeclareSignal<EnemyDiedSignal>();
        container.DeclareSignal<EnemyHit>();
        container.DeclareSignal<InteractPressed>();
        container.DeclareSignal<DashUnlocked>();
        container.DeclareSignal<PlayerDeathVfxFinished>();
        container.DeclareSignal<WeaponSwitched>();
    }

    public static void DeclarePlayerSignals(DiContainer container)
    {
        container.DeclareSignal<GroundedChanged>();
    }
}
