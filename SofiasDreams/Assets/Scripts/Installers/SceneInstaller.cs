using Zenject;

public class SceneInstaller : MonoInstaller
{
    public PlayerFacade playerPrefab;
    public EnemyFacade enemyPrefab;

    public override void InstallBindings()
    {
        // Bus + signals in THIS scene container (source of truth)
        SignalBusInstaller.Install(Container);
        Container.DeclareSignal<AttackStarted>();
        Container.DeclareSignal<AttackFinished>();
        Container.DeclareSignal<HealStarted>();
        Container.DeclareSignal<HealFinished>();
        Container.DeclareSignal<HealInterrupted>();
        Container.DeclareSignal<TookDamage>();
        Container.DeclareSignal<Died>();
        Container.DeclareSignal<PlayerSpawned>();
        Container.DeclareSignal<EnemyKilled>();
        Container.DeclareSignal<HealChargesChanged>();
        Container.DeclareSignal<DashStarted>();   
        Container.DeclareSignal<DashFinished>();
        Container.DeclareSignal<PlayerGrappleRequested>();
        Container.DeclareSignal<GrappleCommand>();
        Container.DeclareSignal<GrappleStarted>();
        Container.DeclareSignal<GrappleFinished>();
        Container.DeclareSignal<EnemyDiedSignal>();

        // Services
        Container.Bind<Spawner>().AsSingle();

        // Factory that spawns the player later
        Container.BindFactory<PlayerFacade, PlayerFactory>()
            .FromComponentInNewPrefab(playerPrefab);
        Container.BindFactory<EnemyFacade, EnemyFactory>()
            .FromComponentInNewPrefab(enemyPrefab)
            .UnderTransformGroup("Enemies");

        // Scene MonoBehaviours that need injection
        Container.BindInterfacesAndSelfTo<CameraTargetBinder>()
            .FromComponentInHierarchy()
            .AsSingle();

        Container.Bind<PlayerHUD>()
            .FromComponentInHierarchy()
            .AsSingle();

        Container.BindInterfacesAndSelfTo<Bootstrapper>()
            .FromComponentInHierarchy()
            .AsSingle();
        
        // Game-over
        Container.BindInterfacesAndSelfTo<PlayerDeathSceneReloader>()
            .AsSingle();
    }
}