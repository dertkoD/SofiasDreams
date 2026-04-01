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
    [SerializeField] string pDagParryBool    = "IsDaggerParry";
    [SerializeField] string stDagParry       = "DaggerParry";
    [SerializeField] string stDagParryFlying = "DaggerParryFlying";

    [Header("Dagger air")]
    [SerializeField] string pDagFlyUpBool   = "DaggerFlyAttackUp";
    [SerializeField] string pDagFlyDownBool = "DaggerFlyAttackDown";
    [SerializeField] string stDagFlyUp      = "DaggerFlyAttackUp";
    [SerializeField] string stDagFlyDown    = "DaggerFlyAttackDown";

    [Header("Sword combo")]
    [SerializeField] string pSwordAtk1 = "IsSwordAttack1";
    [SerializeField] string pSwordAtk2 = "IsSwordAttack2";
    [SerializeField] string pSwordAtk3Trig = "SwordAttack3Trig";
    [SerializeField] string stSwordAtk1 = "SwordAttack1";
    [SerializeField] string stSwordAtk2 = "SwordAttack2";
    [SerializeField] string stSwordAtk3 = "SwordAttack3";

    [Header("Sword dash attack")]
    [SerializeField] string pSwordDashAtkTrig = "SwordDashAttackTrig";
    [SerializeField] string stSwordDashAtk    = "SwordDashAttack";

    [Header("Sword super (charged)")]
    [SerializeField] string pSwordSuperTrig    = "SwordAttackSuperTrig";
    [SerializeField] string stSwordSuper       = "SwordAttackSuper";
    [SerializeField] string pSwordSuperAirTrig = "SwordAttackSuperAirTrig";
    [SerializeField] string stSwordSuperAir    = "SwordAttackSuperAir";

    [Header("Sword air")]
    [SerializeField] string pSwordFlyFwdTrig  = "SwordFlyAttackForwardTrig";
    [SerializeField] string pSwordFlyDownTrig = "SwordFlyAttackDownTrig";
    [SerializeField] string pSwordFlyUpTrig   = "SwordFlyAttackUpTrig";
    [SerializeField] string stSwordFlyFwd     = "SwordFlyAttackForward";
    [SerializeField] string stSwordFlyDown    = "SwordFlyAttackDown";
    [SerializeField] string stSwordFlyUp      = "SwordFlyAttackUp";

    [Header("Tracking Settings")]
    [SerializeField, Range(0.8f, 1.0f)] float clipEndThreshold = 0.98f;
    [SerializeField] float enterTimeout = 0.25f;
    [SerializeField] float safetyTimeout = 2.0f;

    SignalBus _bus;
    PlayerAnimatorConfig _configOverride;
    DaggerCombat _daggerCombat;
    DaggerMomentum _daggerMomentum;
    SwordCombat _swordCombat;

    Coroutine _tUp, _tAirFwd, _tAirDown, _tAirUp, _tHealEnd;
    Coroutine _tChangeWeapon, _tDagSuper, _tDagFlyUp, _tDagFlyDown, _tDagParry;
    Coroutine _tSwordAtk;
    Coroutine _tSwordFlyFwd, _tSwordFlyDown, _tSwordFlyUp;
    Coroutine _tSwordSuper, _tSwordSuperAir;
    Coroutine _tSwordDashAtk;

    [Inject]
    void Construct(
        SignalBus bus,
        [Inject(Optional = true)] PlayerAnimatorConfig injectedConfig = null,
        [Inject(Optional = true)] DaggerCombat daggerCombat = null,
        [Inject(Optional = true)] DaggerMomentum daggerMomentum = null,
        [Inject(Optional = true)] SwordCombat swordCombat = null)
    {
        _bus = bus;
        _configOverride = injectedConfig != null ? injectedConfig : defaultConfig;
        _daggerCombat = daggerCombat;
        _daggerMomentum = daggerMomentum;
        _swordCombat = swordCombat;
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

    public void PlaySwordAttack(int index)
    {
        switch (index)
        {
            case 1:
                SetBool(pSwordAtk1, true);
                SetBool(pSwordAtk2, false);
                break;
            case 2:
                SetBool(pSwordAtk2, true);
                break;
        }

        if (!animator) return;

        if (index == 3)
        {
            animator.SetTrigger(pSwordAtk3Trig);
            Restart(ref _tSwordAtk, TrackExitByName(stSwordAtk3, () =>
                _swordCombat?.FinishSwordStep()));
        }
        else
        {
            string state = index == 1 ? stSwordAtk1 : stSwordAtk2;
            Restart(ref _tSwordAtk, TrackClipEnd(state, () =>
                _swordCombat?.FinishSwordStep()));
        }
    }

    public void PlaySwordDashAttack()
    {
        if (!animator) return;
        animator.Play(stSwordDashAtk, atkLayer, 0f);
        Restart(ref _tSwordDashAtk, TrackExitByName(stSwordDashAtk, () =>
            _bus?.Fire(new AttackFinished { mode = AttackMode.SwordDashAttack, index = 0 })));
    }

    public void PlaySwordSuperAttack()
    {
        if (!animator) return;
        animator.Play(stSwordSuper, atkLayer, 0f);
        Restart(ref _tSwordSuper, TrackExitByName(stSwordSuper, () =>
            _bus?.Fire(new AttackFinished { mode = AttackMode.SwordSuper, index = 0 })));
    }

    public void PlaySwordSuperAirAttack()
    {
        if (!animator) return;
        animator.Play(stSwordSuperAir, atkLayer, 0f);
        Restart(ref _tSwordSuperAir, TrackExitByName(stSwordSuperAir, () =>
            _bus?.Fire(new AttackFinished { mode = AttackMode.SwordSuperAir, index = 0 })));
    }

    public void PlaySwordAirForwardAttack()
    {
        if (!animator) return;
        animator.SetTrigger(pSwordFlyFwdTrig);
        Restart(ref _tSwordFlyFwd, TrackExitByName(stSwordFlyFwd, () =>
            _bus?.Fire(new AttackFinished { mode = AttackMode.SwordAirFwd, index = 0 })));
    }

    public void PlaySwordAirDownAttack()
    {
        if (!animator) return;
        animator.SetTrigger(pSwordFlyDownTrig);
        Restart(ref _tSwordFlyDown, TrackExitByName(stSwordFlyDown, () =>
            _bus?.Fire(new AttackFinished { mode = AttackMode.SwordAirDown, index = 0 })));
    }

    public void PlaySwordAirUpAttack()
    {
        if (!animator) return;
        animator.SetTrigger(pSwordFlyUpTrig);
        Restart(ref _tSwordFlyUp, TrackExitByName(stSwordFlyUp, () =>
            _bus?.Fire(new AttackFinished { mode = AttackMode.SwordAirUp, index = 0 })));
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
        SetBool(pDagParryBool, true);
        Restart(ref _tDagParry, TrackExitByEitherName(stDagParry, stDagParryFlying, () =>
        {
            SetBool(pDagParryBool, false);
            _daggerCombat?.ParryFinishFromAnimation();
        }));
    }

    public void StopDaggerParry()
    {
        if (_tDagParry != null)
        {
            StopCoroutine(_tDagParry);
            _tDagParry = null;
        }
        SetBool(pDagParryBool, false);
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

        pDagParryBool    = config.daggerParryBool;
        stDagParry       = config.daggerParryState;
        stDagParryFlying = config.daggerParryFlyingState;

        pDagFlyUpBool   = config.daggerFlyUpBool;
        pDagFlyDownBool = config.daggerFlyDownBool;
        stDagFlyUp      = config.daggerFlyUpState;
        stDagFlyDown    = config.daggerFlyDownState;

        pSwordAtk1     = config.swordAttack1Bool;
        pSwordAtk2     = config.swordAttack2Bool;
        pSwordAtk3Trig = config.swordAttack3Trig;
        stSwordAtk1    = config.swordAttack1State;
        stSwordAtk2    = config.swordAttack2State;
        stSwordAtk3    = config.swordAttack3State;

        pSwordDashAtkTrig  = config.swordDashAttackTrig;
        stSwordDashAtk     = config.swordDashAttackState;

        pSwordSuperTrig    = config.swordSuperTrig;
        stSwordSuper       = config.swordSuperState;
        pSwordSuperAirTrig = config.swordSuperAirTrig;
        stSwordSuperAir    = config.swordSuperAirState;

        pSwordFlyFwdTrig  = config.swordFlyForwardTrig;
        pSwordFlyDownTrig = config.swordFlyDownTrig;
        pSwordFlyUpTrig   = config.swordFlyUpTrig;
        stSwordFlyFwd     = config.swordFlyForwardState;
        stSwordFlyDown    = config.swordFlyDownState;
        stSwordFlyUp      = config.swordFlyUpState;

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

    IEnumerator TrackExitByEitherName(string stateA, string stateB, Action onExit)
    {
        float t = 0f;
        bool found = false;
        string active = null;
        while (t < enterTimeout)
        {
            var info = animator.GetCurrentAnimatorStateInfo(atkLayer);
            if (info.IsName(stateA)) { active = stateA; found = true; break; }
            if (info.IsName(stateB)) { active = stateB; found = true; break; }
            t += Time.deltaTime;
            yield return null;
        }
        if (!found) { onExit?.Invoke(); yield break; }

        float safe = 0f;
        while (animator.GetCurrentAnimatorStateInfo(atkLayer).IsName(active) && safe < safetyTimeout)
        {
            safe += Time.deltaTime;
            yield return null;
        }
        onExit?.Invoke();
    }

    IEnumerator TrackClipEnd(string stateName, Action onEnd)
    {
        float t = 0f;
        while (!animator.GetCurrentAnimatorStateInfo(atkLayer).IsName(stateName) && t < enterTimeout)
        {
            t += Time.deltaTime;
            yield return null;
        }

        float safe = 0f;
        while (safe < safetyTimeout)
        {
            var info = animator.GetCurrentAnimatorStateInfo(atkLayer);
            if (!info.IsName(stateName)) break;
            if (info.normalizedTime >= clipEndThreshold) break;
            safe += Time.deltaTime;
            yield return null;
        }

        onEnd?.Invoke();
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

        if (e.mode == AttackMode.SwordCombo)
        {
            SetBool(pSwordAtk1, false);
            SetBool(pSwordAtk2, false);
            if (_tSwordAtk != null) { StopCoroutine(_tSwordAtk); _tSwordAtk = null; }
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

        if (e.mode == AttackMode.SwordAirFwd  && _tSwordFlyFwd  != null) { StopCoroutine(_tSwordFlyFwd);  _tSwordFlyFwd  = null; }
        if (e.mode == AttackMode.SwordAirDown && _tSwordFlyDown != null) { StopCoroutine(_tSwordFlyDown); _tSwordFlyDown = null; }
        if (e.mode == AttackMode.SwordAirUp   && _tSwordFlyUp   != null) { StopCoroutine(_tSwordFlyUp);   _tSwordFlyUp   = null; }

        if (e.mode == AttackMode.SwordDashAttack && _tSwordDashAtk   != null) { StopCoroutine(_tSwordDashAtk);   _tSwordDashAtk   = null; }
        if (e.mode == AttackMode.SwordSuper     && _tSwordSuper    != null) { StopCoroutine(_tSwordSuper);    _tSwordSuper    = null; }
        if (e.mode == AttackMode.SwordSuperAir  && _tSwordSuperAir != null) { StopCoroutine(_tSwordSuperAir); _tSwordSuperAir = null; }
    }
}
