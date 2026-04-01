using UnityEngine;

[CreateAssetMenu(menuName = "Configs/Player/Animator", fileName = "PlayerAnimatorConfig")]
public class PlayerAnimatorConfig : ScriptableObject
{
    [Header("Velocity parameters")]
    public string horizontalVelocityParam = "xVelocity";
    public string verticalVelocityParam   = "yVelocity";
    public string groundedBoolParam       = "isJumping";
    public string flyingStateName         = "Flying";
    public string hurtBoolParam           = "Hurt";

    [Header("Combo attacks")]
    public string comboAttack1Bool = "IsAttacking1";
    public string comboAttack2Bool = "IsAttacking2";
    public string comboAttack3Bool = "IsAttacking3";

    [Header("Ground up attack")]
    public string upAttackTrigger  = "UpAttack";
    public int    attackLayerIndex = 0;
    public string upAttackState    = "UpAttack";

    [Header("Air attacks")]
    public string airForwardBool = "JumpAttackForward";
    public string airDownBool    = "JumpAttackDown";
    public string airUpBool      = "JumpAttackUp";

    public string airForwardState = "JumpAttackForward";
    public string airDownState    = "JumpAttackDown";
    public string airUpState      = "JumpAttackUp";

    [Header("Healing")]    
    public string healProcessTrigger = "HealingProcess";
    public string healStartTrigger   = "StartHealing";
    public string healEndTrigger     = "EndHealing";
    public string healEndState       = "EndHealing";

    [Header("Dash & Grapple")]
    public string dashTrigger   = "Dash";
    public string grappleBool   = "isGrappling";

    [Header("Weapon Switch")]
    public string changeWeaponTrigger = "ChangeWeapon";
    public string changeWeaponState   = "ChangeWeapon";

    [Header("Dagger combo")]
    public string daggerAttack1Bool  = "IsDaggerAttack1";
    public string daggerAttack2Bool  = "IsDaggerAttack2";
    public string daggerAttack1State = "DaggerAttack1";
    public string daggerAttack2State = "DaggerAttack2";
    public string daggerSuperTrigger = "DaggerAttackSuperTrig";
    public string daggerSuperState   = "DaggerAttackSuper";

    [Header("Dagger parry")]
    public string daggerParryBool        = "IsDaggerParry";
    public string daggerParryState       = "DaggerParry";
    public string daggerParryFlyingState = "DaggerParryFlying";

    [Header("Dagger air attacks")]
    public string daggerFlyUpBool    = "DaggerFlyAttackUp";
    public string daggerFlyDownBool  = "DaggerFlyAttackDown";
    public string daggerFlyUpState   = "DaggerFlyAttackUp";
    public string daggerFlyDownState = "DaggerFlyAttackDown";

    [Header("Sword combo")]
    public string swordAttack1Bool  = "IsSwordAttack1";
    public string swordAttack2Bool  = "IsSwordAttack2";
    public string swordAttack3Trig  = "SwordAttack3Trig";
    public string swordAttack1State = "SwordAttack1";
    public string swordAttack2State = "SwordAttack2";
    public string swordAttack3State = "SwordAttack3";

    [Header("Sword dash attack")]
    public string swordDashAttackTrig  = "SwordDashAttackTrig";
    public string swordDashAttackState = "SwordDashAttack";

    [Header("Sword super (charged)")]
    public string swordSuperTrig       = "SwordAttackSuperTrig";
    public string swordSuperState      = "SwordAttackSuper";
    public string swordSuperAirTrig    = "SwordAttackSuperAirTrig";
    public string swordSuperAirState   = "SwordAttackSuperAir";

    [Header("Sword air attacks")]
    public string swordFlyForwardTrig = "SwordFlyAttackForwardTrig";
    public string swordFlyDownTrig    = "SwordFlyAttackDownTrig";
    public string swordFlyUpTrig      = "SwordFlyAttackUpTrig";
    public string swordFlyForwardState = "SwordFlyAttackForward";
    public string swordFlyDownState    = "SwordFlyAttackDown";
    public string swordFlyUpState      = "SwordFlyAttackUp";

    [Header("Tracking")]
    [Range(0.5f, 1f)] public float clipEndThreshold = 0.98f;
    [Min(0f)] public float enterTimeout  = 0.25f;
    [Min(0f)] public float safetyTimeout = 2.0f;
}
