using Zenject;

public class SceneInstaller : MonoInstaller
{
    public PlayerFacade playerPrefab;
    public EnemyFacade groundEnemyPrefab;
    public EnemyFacade flyingEnemyPrefab;
    public EnemyFacade jumpingEnemyPrefab;
    public EnemyFacade wormEnemyPrefab;

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

        Container.BindFactory<EnemyFacade, JumpingEnemyFactory>()
            .FromComponentInNewPrefab(jumpingEnemyPrefab)
            .UnderTransformGroup("Enemies_Jumping");

        Container.BindFactory<EnemyFacade, WormEnemyFactory>()
            .FromComponentInNewPrefab(wormEnemyPrefab)
            .UnderTransformGroup("Enemies_Worm");

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