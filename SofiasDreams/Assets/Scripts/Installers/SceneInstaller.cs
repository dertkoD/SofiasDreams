using Zenject;

public class SceneInstaller : MonoInstaller
{
    public PlayerFacade playerPrefab;
    public EnemyFacade groundEnemyPrefab;
    public EnemyFacade flyingEnemyPrefab;

    public override void InstallBindings()
    {
        // Bus + signals in THIS scene container (source of truth)
        SignalBusInstaller.Install(Container);
        PlayerSignalRegistry.DeclareSceneSignals(Container);

        // Services
        Container.Bind<Spawner>().AsSingle();

        // Factory that spawns the player later
        Container.BindFactory<PlayerFacade, PlayerFactory>()
            .FromComponentInNewPrefab(playerPrefab);
        
        Container.BindFactory<EnemyFacade, GroundEnemyFactory>()
            .FromComponentInNewPrefab(groundEnemyPrefab)
            .UnderTransformGroup("Enemies_Ground");

        Container.BindFactory<EnemyFacade, FlyingEnemyFactory>()
            .FromComponentInNewPrefab(flyingEnemyPrefab)
            .UnderTransformGroup("Enemies_Flying");

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