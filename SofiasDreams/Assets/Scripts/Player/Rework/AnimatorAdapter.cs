using System;
using System.Collections;
using UnityEngine;
using Zenject;

public class AnimatorAdapter : MonoBehaviour, IPlayerAnimator, IInitializable, IDisposable
{
    [Header("Refs")]
    [SerializeField] Animator animator;
    [SerializeField] Rigidbody2D rb;
    [SerializeField] PlayerAnimatorConfig defaultConfig;

    [Header("Param names (как в Animator)")]
    [SerializeField] string pX = "xVelocity";
    [SerializeField] string pY = "yVelocity";
    [SerializeField] string pIsJumping = "isJumping";
    [SerializeField] string stFlying = "Flying";
    [SerializeField] string pIsHealing = "isHealing";
    [SerializeField] string pHurt = "Hurt";

    [SerializeField] string pAtk1 = "IsAttacking1";
    [SerializeField] string pAtk2 = "IsAttacking2";
    [SerializeField] string pAtk3 = "IsAttacking3";

    [Header("Ground Up (как было — триггер)")]
    [SerializeField] string trigUp = "UpAttack";
    [SerializeField] int    atkLayer = 0;
    [SerializeField] string stateUp  = "UpAttack"; 

    [Header("AIR attacks")]
    [SerializeField] string pAirFwdBool  = "JumpAttackForward"; 
    [SerializeField] string pAirDownBool = "JumpAttackDown";    
    [SerializeField] string pAirUpBool   = "JumpAttackUp";      

    [SerializeField] string stAirFwd  = "JumpAttackForward";
    [SerializeField] string stAirDown = "JumpAttackDown";
    [SerializeField] string stAirUp   = "JumpAttackUp";
    
    [Header("Healing")]
    [SerializeField] string pHealProcess = "HealingProcess";
    [SerializeField] string pHealStartTrig = "StartHealing";
    [SerializeField] string pHealEndTrig   = "EndHealing";
    [SerializeField] string stHealEnd      = "EndHealing";
    
    [Header("Dashing")]
    [SerializeField] string pDashTrig = "Dash";
    
    [Header("Grapple")]
    [SerializeField] string pIsGrappling = "isGrappling";

    [Header("Weapon Switch")]
    [SerializeField] string pChangeWeaponTrig = "ChangeWeapon";
    [SerializeField] string stChangeWeapon    = "ChangeWeapon";

    [Header("Dagger combo")]
    [SerializeField] string pDagAtk1 = "IsDaggerAttack1";
    [SerializeField] string pDagAtk2 = "IsDaggerAttack2";
    [SerializeField] string stDagAtk1 = "DaggerAttack1";
    [SerializeField] string stDagAtk2 = "DaggerAttack2";
    [SerializeField] string pDagSuperTrig = "DaggerAttackSuperTrig";
    [SerializeField] string stDagSuper    = "DaggerAttackSuper";

    [Header("Dagger parry")]
    [SerializeField] string pDagParryTrig = "DaggerParryTrig";
    [SerializeField] string stDagParry    = "DaggerParry";

    [Header("Dagger air")]
    [SerializeField] string pDagFlyUpBool   = "DaggerFlyAttackUp";
    [SerializeField] string pDagFlyDownBool = "DaggerFlyAttackDown";
    [SerializeField] string stDagFlyUp      = "DaggerFlyAttackUp";
    [SerializeField] string stDagFlyDown    = "DaggerFlyAttackDown";

    [Header("Tracking Settings")]
    [SerializeField, Range(0.8f, 1.0f)] float clipEndThreshold = 0.98f;
    [SerializeField] float enterTimeout = 0.25f;
    [SerializeField] float safetyTimeout = 2.0f;

    SignalBus _bus;
    PlayerAnimatorConfig _configOverride;
    DaggerCombat _daggerCombat;
    DaggerMomentum _daggerMomentum;

    Coroutine _tUp, _tAirFwd, _tAirDown, _tAirUp, _tHealEnd;
    Coroutine _tChangeWeapon, _tDagSuper, _tDagFlyUp, _tDagFlyDown;

    [Inject]
    void Construct(
        SignalBus bus,
        [Inject(Optional = true)] PlayerAnimatorConfig injectedConfig = null,
        [Inject(Optional = true)] DaggerCombat daggerCombat = null,
        [Inject(Optional = true)] DaggerMomentum daggerMomentum = null)
    {
        _bus = bus;
        _configOverride = injectedConfig != null ? injectedConfig : defaultConfig;
        _daggerCombat = daggerCombat;
        _daggerMomentum = daggerMomentum;
    }

    public void Initialize()
    {
        ApplyConfig(_configOverride);

        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!rb) rb = GetComponent<Rigidbody2D>();

        _bus.Subscribe<GroundedChanged>(OnGroundedChanged);
        _bus.Subscribe<AttackFinished>(OnAttackFinished);
        _bus.Subscribe<DashStarted>(OnDashStarted);
        _bus.Subscribe<GrappleStarted>(OnGrappleStarted);
        _bus.Subscribe<GrappleFinished>(OnGrappleFinished);
    }

    public void Dispose()
    {
        _bus.TryUnsubscribe<GroundedChanged>(OnGroundedChanged);
        _bus.TryUnsubscribe<AttackFinished>(OnAttackFinished);
        _bus.TryUnsubscribe<DashStarted>(OnDashStarted);
        _bus.TryUnsubscribe<GrappleStarted>(OnGrappleStarted);
        _bus.TryUnsubscribe<GrappleFinished>(OnGrappleFinished);
    }

    void Update()
    {
        if (!animator || !rb) return;
        animator.SetFloat(pX, Mathf.Abs(rb.linearVelocity.x));
        animator.SetFloat(pY, rb.linearVelocity.y);
    }

    // ───────── IPlayerAnimator ─────────
    public void SetMoveSpeed(float s01)          => animator?.SetFloat(pX, s01);
    public void SetGrounded(bool grounded)       => animator?.SetBool(pIsJumping, !grounded);

    public void PlayAttack(int index)
    {
        SetBool(pAtk1, index == 1);
        SetBool(pAtk2, index == 2);
        SetBool(pAtk3, index == 3);
    }

    public void PlayUpAttack()
    {
        if (!animator) return;
        animator.SetTrigger(trigUp);
        Restart(ref _tUp, TrackExitByName(stateUp, () =>
            _bus?.Fire(new AttackFinished { mode = AttackMode.Up, index = 0 })));
    }
    
    public void PlayAirForwardAttack()
    {
        if (!animator) return;
        SetBool(pAirFwdBool, true);
        Restart(ref _tAirFwd, TrackAirBool(stAirFwd, pAirFwdBool, AttackMode.AirFwd));
    }

    public void PlayAirDownAttack()
    {
        if (!animator) return;
        SetBool(pAirDownBool, true);
        Restart(ref _tAirDown, TrackAirBool(stAirDown, pAirDownBool, AttackMode.AirDown));
    }

    public void PlayAirUpAttack()
    {
        if (!animator) return;
        SetBool(pAirUpBool, true);
        Restart(ref _tAirUp, TrackAirBool(stAirUp, pAirUpBool, AttackMode.AirUp));
    }

    // ───────── Dagger ─────────

    public void PlayDaggerAttack(int index)
    {
        SetBool(pDagAtk1, index == 1);
        SetBool(pDagAtk2, index == 2);

        if (!animator) return;
        ApplyMomentumSpeed();
        string state = index == 1 ? stDagAtk1 : stDagAtk2;
        animator.Play(state, atkLayer, 0f);

        if (index == 2)
            Restart(ref _tDagSuper, TrackExitByName(stDagAtk2, () =>
            {
                SetBool(pDagAtk2, false);
                RestoreAnimatorSpeed();
                _daggerCombat?.DaggerFinishFromAnimation();
            }));
    }

    public void PlayDaggerSuperAttack()
    {
        if (!animator) return;
        SetBool(pDagAtk1, false);
        SetBool(pDagAtk2, false);
        animator.SetTrigger(pDagSuperTrig);
        Restart(ref _tDagSuper, TrackExitByName(stDagSuper, () =>
            _daggerCombat?.DaggerFinishFromAnimation()));
    }

    public void PlayDaggerParry()
    {
        if (!animator) return;
        animator.SetTrigger(pDagParryTrig);
    }

    public void PlayDaggerFlyAttackUp()
    {
        if (!animator) return;
        SetBool(pDagFlyUpBool, true);
        Restart(ref _tDagFlyUp, TrackAirBool(stDagFlyUp, pDagFlyUpBool, AttackMode.DaggerFlyUp));
    }

    public void PlayDaggerFlyAttackDown()
    {
        if (!animator) return;
        SetBool(pDagFlyDownBool, true);
        Restart(ref _tDagFlyDown, TrackAirBool(stDagFlyDown, pDagFlyDownBool, AttackMode.DaggerFlyDown));
    }

    public void PlayChangeWeapon(Action onComplete = null)
    {
        if (!animator)
        {
            onComplete?.Invoke();
            return;
        }
        animator.SetTrigger(pChangeWeaponTrig);
        Restart(ref _tChangeWeapon, TrackExitByName(stChangeWeapon, onComplete));
    }

    public void PlayHealStart()
    {
        if (!animator) return;

        animator.SetTrigger(pHealProcess);
        animator.SetTrigger(pHealStartTrig);
    }

    public void PlayHealEnd(bool interrupted, Action onComplete = null)
    {
        if (!animator) 
        {
            onComplete?.Invoke();
            return;
        }

        animator.SetTrigger(pHealProcess);
        animator.SetTrigger(pHealEndTrig);
        
        Restart(ref _tHealEnd, TrackExitByName(stHealEnd, onComplete));
    }
    
    void OnDashStarted(DashStarted s)
    {
        if (!animator) return;
        animator.SetTrigger(pDashTrig);
    }
    
    public void PlayHurt()             => animator?.SetBool(pHurt, true);
    public void PlayDeath()            => animator?.SetBool(pHurt, true);
    
    // ───────── Signal handlers ─────────

    void OnGroundedChanged(GroundedChanged g) => SetGrounded(g.grounded);

    void OnGrappleStarted(GrappleStarted s)
    {
        if (!animator) return;
        // Just set a bool; you can also add a trigger here if your state machine needs it
        animator.SetBool(pIsGrappling, true);
    }

    void OnGrappleFinished(GrappleFinished s)
    {
        if (!animator) return;
        animator.SetBool(pIsGrappling, false);
    }

    // ───────── Helpers ─────────

    void SetBool(string name, bool v) { if (animator) animator.SetBool(name, v); }

    void ApplyMomentumSpeed()
    {
        if (!animator || _daggerMomentum == null) return;
        animator.speed = _daggerMomentum.SpeedMultiplier;
    }

    void RestoreAnimatorSpeed()
    {
        if (!animator) return;
        animator.speed = 1f;
    }

    void ApplyConfig(PlayerAnimatorConfig config)
    {
        if (config == null)
            return;

        pX = config.horizontalVelocityParam;
        pY = config.verticalVelocityParam;
        pIsJumping = config.groundedBoolParam;
        stFlying = config.flyingStateName;
        pHurt = config.hurtBoolParam;

        pAtk1 = config.comboAttack1Bool;
        pAtk2 = config.comboAttack2Bool;
        pAtk3 = config.comboAttack3Bool;

        trigUp = config.upAttackTrigger;
        atkLayer = config.attackLayerIndex;
        stateUp = config.upAttackState;

        pAirFwdBool = config.airForwardBool;
        pAirDownBool = config.airDownBool;
        pAirUpBool = config.airUpBool;

        stAirFwd = config.airForwardState;
        stAirDown = config.airDownState;
        stAirUp = config.airUpState;

        pHealProcess = config.healProcessTrigger;
        pHealStartTrig = config.healStartTrigger;
        pHealEndTrig = config.healEndTrigger;
        stHealEnd    = config.healEndState;

        pDashTrig = config.dashTrigger;
        pIsGrappling = config.grappleBool;

        pChangeWeaponTrig = config.changeWeaponTrigger;
        stChangeWeapon    = config.changeWeaponState;

        pDagAtk1        = config.daggerAttack1Bool;
        pDagAtk2        = config.daggerAttack2Bool;
        stDagAtk1       = config.daggerAttack1State;
        stDagAtk2       = config.daggerAttack2State;
        pDagSuperTrig   = config.daggerSuperTrigger;
        stDagSuper      = config.daggerSuperState;

        pDagParryTrig   = config.daggerParryTrigger;
        stDagParry      = config.daggerParryState;

        pDagFlyUpBool   = config.daggerFlyUpBool;
        pDagFlyDownBool = config.daggerFlyDownBool;
        stDagFlyUp      = config.daggerFlyUpState;
        stDagFlyDown    = config.daggerFlyDownState;

        clipEndThreshold = config.clipEndThreshold;
        enterTimeout = config.enterTimeout;
        safetyTimeout = config.safetyTimeout;
    }

    void Restart(ref Coroutine slot, IEnumerator co)
    {
        if (slot != null) StopCoroutine(slot);
        slot = StartCoroutine(co);
    }

    IEnumerator TrackExitByName(string stateName, Action onExit)
    {
        float t = 0f;
        while (!animator.GetCurrentAnimatorStateInfo(atkLayer).IsName(stateName) && t < enterTimeout)
        {
            t += Time.deltaTime;
            yield return null;
        }
        float safe = 0f;
        while (animator.GetCurrentAnimatorStateInfo(atkLayer).IsName(stateName) && safe < safetyTimeout)
        {
            safe += Time.deltaTime;
            yield return null;
        }
        onExit?.Invoke();
    }

    IEnumerator TrackAirBool(string stateName, string boolParam, AttackMode mode)
    {
        float tEnter = 0f;
        while (!animator.GetCurrentAnimatorStateInfo(atkLayer).IsName(stateName) &&
               tEnter < enterTimeout)
        {
            tEnter += Time.deltaTime;
            yield return null;
        }
        if (!animator.GetCurrentAnimatorStateInfo(atkLayer).IsName(stateName))
            yield break;

        float safe = 0f;
        while (safe < safetyTimeout)
        {
            var st = animator.GetCurrentAnimatorStateInfo(atkLayer);
            if (st.IsName(stateName) && st.normalizedTime >= clipEndThreshold) break;
            safe += Time.deltaTime;
            yield return null;
        }

        SetBool(boolParam, false);

        bool left = false;
        for (int i = 0; i < 2; i++)
        {
            yield return null; 
            if (!animator.GetCurrentAnimatorStateInfo(atkLayer).IsName(stateName))
            {
                left = true;
                break;
            }
        }

        if (!left && !string.IsNullOrEmpty(stFlying))
        {
            animator.CrossFadeInFixedTime(stFlying, 0.05f, atkLayer);
            yield return null;
        }

        _bus?.Fire(new AttackFinished { mode = mode, index = 0 });
    }

    void OnAttackFinished(AttackFinished e)
    {
        if (e.mode == AttackMode.Combo)
        {
            SetBool(pAtk1, false);
            SetBool(pAtk2, false);
            SetBool(pAtk3, false);
        }

        if (e.mode is AttackMode.DaggerCombo or AttackMode.DaggerSuper)
        {
            SetBool(pDagAtk1, false);
            SetBool(pDagAtk2, false);
            RestoreAnimatorSpeed();
        }

        if (e.mode == AttackMode.Up     && _tUp      != null) { StopCoroutine(_tUp);      _tUp      = null; }
        if (e.mode == AttackMode.AirFwd && _tAirFwd  != null) { StopCoroutine(_tAirFwd);  _tAirFwd  = null; SetBool(pAirFwdBool,  false); }
        if (e.mode == AttackMode.AirDown&& _tAirDown != null) { StopCoroutine(_tAirDown); _tAirDown = null; SetBool(pAirDownBool, false); }
        if (e.mode == AttackMode.AirUp  && _tAirUp   != null) { StopCoroutine(_tAirUp);   _tAirUp   = null; SetBool(pAirUpBool,   false); }

        if (e.mode == AttackMode.DaggerSuper  && _tDagSuper  != null) { StopCoroutine(_tDagSuper);  _tDagSuper  = null; }
        if (e.mode == AttackMode.DaggerFlyUp  && _tDagFlyUp  != null) { StopCoroutine(_tDagFlyUp);  _tDagFlyUp  = null; SetBool(pDagFlyUpBool,   false); }
        if (e.mode == AttackMode.DaggerFlyDown&& _tDagFlyDown!= null) { StopCoroutine(_tDagFlyDown); _tDagFlyDown= null; SetBool(pDagFlyDownBool, false); }
    }
}
