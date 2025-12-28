using Zenject;

public class BonfireInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        // Signals
        Container.DeclareSignal<BonfireRestStateChanged>();
        Container.DeclareSignal<BonfireCheckpointChanged>();
        Container.DeclareSignal<PlayerRespawnedAtBonfire>();

        // Registry
        Container.BindInterfacesAndSelfTo<BonfireResetRegistry>().AsSingle();

        // Enemy safety gate
        Container.Bind<IEnemyCombatGate>().To<EnemyCombatGate>().AsSingle();

        // These MUST match your project:
        // IMobilityGate should already be bound in your player installer.
        // IPlayerVitals you will implement as an adapter to your Health+Heals.
        Container.Bind<IPlayerVitals>().To<PlayerVitalsAdapter>().AsSingle();

        // Bonfire service
        Container.BindInterfacesTo<BonfireService>().AsSingle();
        // Container.BindInterfacesTo<BonfireRespawnOnDeath>().AsSingle(); // Moved to SceneInstaller
    }
}