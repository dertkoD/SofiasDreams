using System;
using System.Collections;
using UnityEngine;
using Zenject;

public class AnimatorAdapter : MonoBehaviour, IPlayerAnimator, IInitializable, IDisposable
{
    [Header("Refs")]
    [SerializeField] Animator animator;
    [SerializeField] Rigidbody2D rb;

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
    
    [Header("Dashing")]
    [SerializeField] string pDashTrig = "Dash";
    
    [Header("Grapple")]
    [SerializeField] string pIsGrappling = "isGrappling";

    [Header("Tracking Settings")]
    [SerializeField, Range(0.8f, 1.0f)] float clipEndThreshold = 0.98f; 
    [SerializeField] float enterTimeout = 0.25f;   
    [SerializeField] float safetyTimeout = 2.0f;  

    SignalBus _bus;

    Coroutine _tUp, _tAirFwd, _tAirDown, _tAirUp;

    [Inject] void Inject(SignalBus bus) => _bus = bus;

    public void Initialize()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!rb) rb = GetComponent<Rigidbody2D>();

        _bus.Subscribe<GroundedChanged>(g => SetGrounded(g.grounded));
        _bus.Subscribe<AttackFinished>(OnAttackFinished);
        _bus.Subscribe<DashStarted>(OnDashStarted);
        _bus.Subscribe<GrappleStarted>(OnGrappleStarted);
        _bus.Subscribe<GrappleFinished>(OnGrappleFinished);
    }

    public void Dispose()
    {
        _bus.TryUnsubscribe<GroundedChanged>(g => SetGrounded(g.grounded));
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
    
    public void PlayHealStart()
    {
        if (!animator) return;

        animator.SetTrigger(pHealProcess);
        animator.SetTrigger(pHealStartTrig);
    }

    public void PlayHealEnd(bool interrupted)
    {
        if (!animator) return;

        animator.SetTrigger(pHealProcess);
        animator.SetTrigger(pHealEndTrig);
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

        if (e.mode == AttackMode.Up     && _tUp      != null) { StopCoroutine(_tUp);      _tUp      = null; }
        if (e.mode == AttackMode.AirFwd && _tAirFwd  != null) { StopCoroutine(_tAirFwd);  _tAirFwd  = null; SetBool(pAirFwdBool,  false); }
        if (e.mode == AttackMode.AirDown&& _tAirDown != null) { StopCoroutine(_tAirDown); _tAirDown = null; SetBool(pAirDownBool, false); }
        if (e.mode == AttackMode.AirUp  && _tAirUp   != null) { StopCoroutine(_tAirUp);   _tAirUp   = null; SetBool(pAirUpBool,   false); }
    }
}
