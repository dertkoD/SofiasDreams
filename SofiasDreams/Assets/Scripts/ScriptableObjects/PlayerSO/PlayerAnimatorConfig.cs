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

    [Header("Dash & Grapple")]
    public string dashTrigger   = "Dash";
    public string grappleBool   = "isGrappling";

    [Header("Tracking")]   
    [Range(0.5f, 1f)] public float clipEndThreshold = 0.98f;
    [Min(0f)] public float enterTimeout  = 0.25f;
    [Min(0f)] public float safetyTimeout = 2.0f;
}
