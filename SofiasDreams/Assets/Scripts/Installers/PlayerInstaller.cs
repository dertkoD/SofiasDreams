using UnityEngine;
using Zenject;

public class PlayerInstaller : MonoInstaller
{
    [Header("Configs (SO)")]
    public PlayerMovementConfig moveSO;
    public PlayerJumpConfig    jumpSO;
    public PlayerAttackConfig  attackSO;
    public PlayerHealConfig    healSO;
    public PlayerHealthConfig  healthSO;
    public PlayerUpAttackConfig upAttackSO;
    public PlayerDashConfig    dashSO;
    public PlayerGrappleConfig grappleSO;
    public HitReactionConfig hitReactionSO;
    public PlayerAnimatorConfig animatorSO;
    public PlayerWeaponConfig weaponSO;

    public override void InstallBindings()
    {
        Container.Bind<IInputService>().To<InputService>().AsSingle();

        Container.BindInterfacesAndSelfTo<PlayerStateMachine>().AsSingle();
        Container.Bind<IPlayerAbilityConfigurator>().To<PlayerAbilityConfigurator>().AsSingle();

        Container.Bind<IMobilityGate>().To<MobilityGate>().AsSingle();

        PlayerSignalRegistry.DeclarePlayerSignals(Container);

        Container.BindInterfacesAndSelfTo<Mover2D>().FromComponentOnRoot().AsSingle();
        Container.BindInterfacesAndSelfTo<Jumper2D>().FromComponentOnRoot().AsSingle();
        Container.BindInterfacesAndSelfTo<Combat3>().FromComponentOnRoot().AsSingle();
        Container.BindInterfacesAndSelfTo<Healer>().FromComponentOnRoot().AsSingle();
        Container.BindInterfacesAndSelfTo<Health>().FromComponentOnRoot().AsSingle();
        Container.BindInterfacesAndSelfTo<Knockback2D>().FromComponentOnRoot().AsSingle();
        Container.BindInterfacesAndSelfTo<AnimatorAdapter>().FromComponentOnRoot().AsSingle();
        Container.BindInterfacesAndSelfTo<InputAdapter>().FromComponentOnRoot().AsSingle();
        Container.BindInterfacesAndSelfTo<Dasher2D>().FromComponentOnRoot().AsSingle();
        Container.BindInterfacesAndSelfTo<Grappler2D>().FromComponentOnRoot().AsSingle();
        Container.BindInterfacesAndSelfTo<UpAttack>().FromComponentOnRoot().AsSingle();
        Container.BindInterfacesAndSelfTo<GrappleHairTrailSprites>().FromComponentOnRoot().AsSingle();

        if (moveSO)     Container.BindInstance(moveSO);
        if (jumpSO)     Container.BindInstance(jumpSO);
        if (attackSO)   Container.BindInstance(attackSO);
        if (healSO)     Container.BindInstance(healSO);
        if (healthSO)   Container.BindInstance(healthSO);
        if (upAttackSO) Container.BindInstance(upAttackSO);
        if (dashSO)     Container.BindInstance(dashSO);
        if (grappleSO)  Container.BindInstance(grappleSO);
        if (hitReactionSO) Container.BindInstance(hitReactionSO);
        if (animatorSO) Container.BindInstance(animatorSO);
        if (weaponSO)   Container.BindInstance(weaponSO);
    }
}