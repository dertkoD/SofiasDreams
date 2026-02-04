using Zenject;

public class SceneInstaller : MonoInstaller
{
    public PlayerFacade playerPrefab;
    public EnemyFacade groundEnemyPrefab;
    public EnemyFacade flyingEnemyPrefab;
    public EnemyFacade jumpingEnemyPrefab;
    public EnemyFacade wormEnemyPrefab;
    public EnemyFacade swarmEnemyPrefab;
    public EnemyFacade lazyMiniBossPrefab;

    public override void InstallBindings()
    {
        // Bus + signals in THIS scene container (source of truth)
        SignalBusInstaller.Install(Container);
        PlayerSignalRegistry.DeclareSceneSignals(Container);

        // Services
        Container.Bind<Spawner>().AsSingle();
        
        Container.Bind<IPlayerAbilities>().To<PlayerAbilities>().AsSingle();

        // Factory that spawns the player later
        Container.BindFactory<PlayerFacade, PlayerFactory>()
            .FromComponentInNewPrefab(playerPrefab);
        
        Container.BindFactory<EnemyFacade, GroundEnemyFactory>()
            .FromComponentInNewPrefab(groundEnemyPrefab)
            .UnderTransformGroup("Enemies_Ground");

        Container.BindFactory<EnemyFacade, FlyingEnemyFactory>()
            .FromComponentInNewPrefab(flyingEnemyPrefab)
            .UnderTransformGroup("Enemies_Flying");

        Container.BindFactory<EnemyFacade, JumpingEnemyFactory>()
            .FromComponentInNewPrefab(jumpingEnemyPrefab)
            .UnderTransformGroup("Enemies_Jumping");

        Container.BindFactory<EnemyFacade, WormEnemyFactory>()
            .FromComponentInNewPrefab(wormEnemyPrefab)
            .UnderTransformGroup("Enemies_Worm");
        
        Container.BindFactory<EnemyFacade, SwarmEnemyFactory>()
            .FromComponentInNewPrefab(swarmEnemyPrefab)
            .UnderTransformGroup("Enemies_Swarm");
        
        Container.BindFactory<EnemyFacade, LazyMiniBossFactory>()
            .FromComponentInNewPrefab(lazyMiniBossPrefab)
            .UnderTransformGroup("Enemies_LazyMiniBoss");

        // Scene MonoBehaviours that need injection
        Container.BindInterfacesAndSelfTo<ShockWaveSpriteController>()
            .FromComponentInHierarchy()
            .AsSingle();
        
        Container.Bind<PlayerHUD>()
            .FromComponentInHierarchy()
            .AsSingle();

        Container.BindInterfacesAndSelfTo<Bootstrapper>()
            .FromComponentInHierarchy()
            .AsSingle();
        
        // Bonfire signals
        Container.DeclareSignal<BonfireRestStateChanged>();
        Container.DeclareSignal<BonfireCheckpointChanged>();
        Container.DeclareSignal<PlayerRespawnedAtBonfire>();
        Container.DeclareSignal<BonfireRespawnRequested>();
        Container.DeclareSignal<BonfireEnemiesRespawnRequested>();
        Container.DeclareSignal<BossFloorBrokenSignal>();

        // Bonfire services
        Container.BindInterfacesAndSelfTo<BonfireService>().AsSingle();
        Container.BindInterfacesAndSelfTo<BonfireRespawnOnDeath>().AsSingle();

        Container.Bind<IEnemyCombatGate>().To<EnemyCombatGate>().AsSingle();
        
        // spawn meta
        
        Container.DeclareSignal<EnemyKilledSignal>();
        Container.BindInterfacesAndSelfTo<EnemyKilledPersistenceListener>().AsSingle();
        Container.Bind<IEnemyPersistenceService>().To<EnemyPersistenceService>().AsSingle();
        
    }
}