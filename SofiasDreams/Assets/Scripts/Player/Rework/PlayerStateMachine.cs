using System;
using UnityEngine;
using Zenject;

public class PlayerStateMachine : IPlayerCommands, IInitializable, IDisposable, ITickable
{
    PlayerState _state = PlayerState.Idle;

    // Core
    readonly SignalBus _bus;
    readonly IMobilityGate _gate;

    // Components
    readonly Mover2D        _mover;
    readonly Jumper2D       _jumper;
    readonly ICombat        _combo;
    readonly SwordCombat    _swordCombat;
    readonly DaggerCombat   _daggerCombat;
    readonly Healer         _healer;
    readonly Health         _health;
    readonly Knockback2D    _knock;
    readonly IPlayerAnimator _anim;
    readonly Dasher2D       _dasher;
    readonly Grappler2D   _grappler;
    readonly IJumpAttack    _jumpAttack;
    readonly PlayerInteractor _interactor;

    readonly IPlayerAbilities _abilities;
    readonly IPlayerAbilityConfigurator _abilityConfigurator;
    readonly IWeaponManager _weaponManager;
    readonly HitReactionConfig _hitSO;
    readonly IBonfireService _bonfire;

    float       _moveX;
    AttackMode? _activeAttack;
    bool        _isCharging;

    public PlayerStateMachine(
        SignalBus bus, IMobilityGate gate,
        Mover2D mover, Jumper2D jumper,
        ICombat combo, SwordCombat swordCombat, DaggerCombat daggerCombat,
        Healer healer, Health health, Knockback2D knock, IPlayerAnimator anim,
        Dasher2D dasher, Grappler2D grappler, IJumpAttack jumpAttack, PlayerInteractor interactor,
        IPlayerAbilities abilities, IPlayerAbilityConfigurator abilityConfigurator,
        IWeaponManager weaponManager,
        IBonfireService bonfire,
        [Inject(Optional = true)] HitReactionConfig hitSO)
    {
        _bus                 = bus;
        _gate                = gate;
        _mover               = mover;
        _jumper              = jumper;
        _combo               = combo;
        _swordCombat         = swordCombat;
        _daggerCombat        = daggerCombat;
        _healer              = healer;
        _health              = health;
        _knock               = knock;
        _anim                = anim;
        _dasher              = dasher;
        _grappler            = grappler;
        _jumpAttack          = jumpAttack;
        _interactor          = interactor;
        _abilities           = abilities;
        _abilityConfigurator = abilityConfigurator;
        _weaponManager       = weaponManager;
        _bonfire             = bonfire;
        _hitSO               = hitSO;
    }

    public void Initialize()
    {
        _abilityConfigurator?.Configure();

        _bus.Subscribe<AttackStarted>(OnAttackStarted);
        _bus.Subscribe<AttackFinished>(OnAttackFinished);
        _bus.Subscribe<HealStarted>(OnHealStarted);
        _bus.Subscribe<HealFinished>(OnHealFinished);
        _bus.Subscribe<HealInterrupted>(OnHealInterrupted);
        _bus.Subscribe<TookDamage>(OnTookDamage);
        _bus.Subscribe<Died>(OnDied);
        _bus.Subscribe<GroundedChanged>(OnGroundedChanged);
        _bus.Subscribe<DashStarted>(OnDashStarted);
        _bus.Subscribe<DashFinished>(OnDashFinished);
        _bus.Subscribe<PlayerGrappleRequested>(OnGrappleRequested);
        _bus.Subscribe<GrappleFinished>(OnGrappleFinished);
        _bus.Subscribe<InteractPressed>(OnInteractPressed);
        _bus.Subscribe<BonfireRestStateChanged>(OnBonfireRestStateChanged);
        _bus.Subscribe<ParryFinished>(OnParryFinished);
    }

    public void Dispose()
    {
        _bus.TryUnsubscribe<AttackStarted>(OnAttackStarted);
        _bus.TryUnsubscribe<AttackFinished>(OnAttackFinished);
        _bus.TryUnsubscribe<HealStarted>(OnHealStarted);
        _bus.TryUnsubscribe<HealFinished>(OnHealFinished);
        _bus.TryUnsubscribe<HealInterrupted>(OnHealInterrupted);
        _bus.TryUnsubscribe<TookDamage>(OnTookDamage);
        _bus.TryUnsubscribe<Died>(OnDied);
        _bus.TryUnsubscribe<GroundedChanged>(OnGroundedChanged);
        _bus.TryUnsubscribe<DashStarted>(OnDashStarted);
        _bus.TryUnsubscribe<DashFinished>(OnDashFinished);
        _bus.TryUnsubscribe<PlayerGrappleRequested>(OnGrappleRequested);
        _bus.TryUnsubscribe<GrappleFinished>(OnGrappleFinished);
        _bus.TryUnsubscribe<InteractPressed>(OnInteractPressed);
        _bus.TryUnsubscribe<BonfireRestStateChanged>(OnBonfireRestStateChanged);
        _bus.TryUnsubscribe<ParryFinished>(OnParryFinished);
    }

    // ───────────────────── Commands ─────────────────────

    public void Move(float x)
    {
        if (_state == PlayerState.Dead || _state == PlayerState.Dash
            || _state == PlayerState.BonfireRest || _state == PlayerState.ChangeWeapon)
            return;
        
        _moveX = x;

        if (_gate.IsMovementBlocked) return;

        _mover.SetInput(x);
        _anim.SetMoveSpeed(Mathf.Abs(x));

        if (Mathf.Abs(x) > 0.01f)
        {
            if (_state is PlayerState.Idle or PlayerState.Move)
                _state = PlayerState.Move;
        }
        else
        {
            Stop();
        }
    }

    public void Stop()
    {
        if (_state == PlayerState.Dead) return;
        _moveX = 0f;
        _mover.SetInput(0f);
        if (_state == PlayerState.Move)
            _state = PlayerState.Idle;
        _anim.SetMoveSpeed(0f);
    }

    public void Jump()
    {
        if (_state == PlayerState.Dead ||
            _state == PlayerState.Hurt ||
            _state == PlayerState.Heal ||
            _state == PlayerState.Dash ||
            _state == PlayerState.Attack ||
            _state == PlayerState.Grapple ||
            _state == PlayerState.BonfireRest ||
            _state == PlayerState.ChangeWeapon)
            return;
        
        if (_gate.IsJumpBlocked) return;

        _jumper.RequestJump();
        _state = PlayerState.Jump;
    }
    
    public void JumpRelease()
    {
        if (_state == PlayerState.Dead || _state == PlayerState.BonfireRest)
            return;

        if (_gate.IsJumpBlocked)
            return;

        _jumper.NotifyJumpReleased();
    }

    bool TryBufferDashAttack()
    {
        if (_state != PlayerState.Dash) return false;
        if (_weaponManager.CurrentWeapon == WeaponType.Sword)
            _swordCombat.BufferDashAttack();
        return true;
    }

    public void Attack()
    {
        if (_state == PlayerState.Dead) return;
        if (_state is PlayerState.Heal or PlayerState.Hurt or PlayerState.BonfireRest or PlayerState.ChangeWeapon) return;
        if (TryBufferDashAttack()) return;

        if (_jumper.IsGrounded)
            _mover.StopHorizontal();

        Block(MobilityBlockReason.Attack);

        switch (_weaponManager.CurrentWeapon)
        {
            case WeaponType.Dagger:
                _daggerCombat.RequestAttack();
                break;
            case WeaponType.Sword:
                _swordCombat.RequestAttack();
                break;
            default:
                _combo.RequestAttack();
                break;
        }

        _state = PlayerState.Attack;
    }

    public void UpAttack()
    {
        if (_state == PlayerState.Dead) return;
        if (_state is PlayerState.Heal or PlayerState.Hurt or PlayerState.BonfireRest or PlayerState.ChangeWeapon) return;
        if (TryBufferDashAttack()) return;

        if (_weaponManager.CurrentWeapon == WeaponType.Dagger)
        {
            if (!_jumper.IsGrounded)
            {
                if (_jumpAttack.Request(AttackMode.DaggerFlyUp))
                    Block(MobilityBlockReason.Attack);
            }
            return;
        }

        if (_weaponManager.CurrentWeapon == WeaponType.Sword)
        {
            if (!_jumper.IsGrounded)
            {
                if (_jumpAttack.Request(AttackMode.SwordAirUp))
                    Block(MobilityBlockReason.Attack);
            }
            return;
        }

        if (_jumper.IsGrounded)
        {
            Block(MobilityBlockReason.Attack);
            _mover.StopHorizontal();
            _bus.Fire(new AttackStarted { mode = AttackMode.Up, index = 0 });
        }
        else
        {
            if (_jumpAttack.Request(AttackMode.AirUp))
                Block(MobilityBlockReason.Attack);
        }
    }

    public void ForwardJumpAttack()
    {
        if (_state == PlayerState.Dead) return;
        if (TryBufferDashAttack()) return;
        if (_jumper.IsGrounded) return;
        if (_state is PlayerState.Heal or PlayerState.Hurt or PlayerState.BonfireRest or PlayerState.ChangeWeapon) return;

        var mode = _weaponManager.CurrentWeapon switch
        {
            WeaponType.Dagger => AttackMode.DaggerFlyDown,
            WeaponType.Sword  => AttackMode.SwordAirFwd,
            _                 => AttackMode.AirFwd
        };
        _jumpAttack.Request(mode);
    }

    public void UpJumpAttack()
    {
        if (_state == PlayerState.Dead) return;
        if (TryBufferDashAttack()) return;
        if (_jumper.IsGrounded) return;
        if (_state is PlayerState.Heal or PlayerState.Hurt or PlayerState.BonfireRest or PlayerState.ChangeWeapon) return;

        var mode = _weaponManager.CurrentWeapon switch
        {
            WeaponType.Dagger => AttackMode.DaggerFlyUp,
            WeaponType.Sword  => AttackMode.SwordAirUp,
            _                 => AttackMode.AirUp
        };
        _jumpAttack.Request(mode);
    }

    public void DownJumpAttack()
    {
        if (_state == PlayerState.Dead) return;
        if (TryBufferDashAttack()) return;
        if (_jumper.IsGrounded) return;
        if (_state is PlayerState.Heal or PlayerState.Hurt or PlayerState.BonfireRest or PlayerState.ChangeWeapon) return;

        var mode = _weaponManager.CurrentWeapon switch
        {
            WeaponType.Dagger => AttackMode.DaggerFlyDown,
            WeaponType.Sword  => AttackMode.SwordAirDown,
            _                 => AttackMode.AirDown
        };
        _jumpAttack.Request(mode);
    }

    public void HealBegin()
    {
        if (_state == PlayerState.Dead || _state == PlayerState.Hurt || _state == PlayerState.BonfireRest)
            return;

        _healer.StartHeal();
    }

    public void HealCancel()
    {
        _healer.CancelHealing();
    }

    public void DropPlatform()
    {
        if (_state == PlayerState.Dead ||
            _state == PlayerState.Hurt ||
            _state == PlayerState.Heal ||
            _state == PlayerState.Attack ||
            _state == PlayerState.Dash ||
            _state == PlayerState.Grapple ||
            _state == PlayerState.BonfireRest)
            return;

        if (!_jumper.IsGrounded)
            return;

        _jumper.RequestDropThrough();

        // Treat as airborne for logic until grounded signal fires
        _state = PlayerState.Jump;
    }

    void OnInteractPressed(InteractPressed _)
    {
        
        if (_state == PlayerState.Dead || _state == PlayerState.Hurt || _state == PlayerState.Dash ||
            _state == PlayerState.Attack || _state == PlayerState.Grapple || _state == PlayerState.Heal)
            return;

        bool ok = _interactor != null && _interactor.TryInteract(_mover.transform);
        Debug.Log("[Interact] TryInteract => " + ok);
    }
    
    public void Interact()
    {
        _bus.Fire(new InteractPressed());
    }

    public void ApplyDamage(DamageInfo info)
    {
        if (EnemyCombatGate.IsBonfireSafe)
            return;

        if (_state == PlayerState.Dead || _state == PlayerState.BonfireRest)
            return;

        if (_daggerCombat.IsParrying && _weaponManager.CurrentWeapon == WeaponType.Dagger
            && info.source != null)
        {
            _daggerCombat.TryExecuteParry(info.source);
            return;
        }

        if (_health.IsInvincible && !info.bypassInvuln)
            return;

        if (_hitSO != null)
        {
            if (info.stunSeconds <= 0f)
                info.stunSeconds = _hitSO.hitStun;

            if (info.impulse == Vector2.zero)
            {
                Vector2 dir;

                if (info.hitNormal != Vector2.zero)
                {
                    dir = -info.hitNormal.normalized;
                }
                else if (info.hitPoint != Vector2.zero)
                {
                    var center = (Vector2)_mover.transform.position;
                    dir = (center - info.hitPoint).normalized;
                }
                else
                {
                    int facing = _mover.FacingDir; 
                    dir = new Vector2(-facing, 0f);
                }

                info.impulse = dir * _hitSO.knockbackForce;
            }
        }

        _health.ApplyDamage(info);
        _mover.StopHorizontal();
        _knock.Apply(info);
    }

    public void Parry()
    {
        if (_state == PlayerState.Dead) return;
        if (_state is PlayerState.Heal or PlayerState.Hurt or PlayerState.Dash
            or PlayerState.Grapple or PlayerState.BonfireRest or PlayerState.ChangeWeapon) return;

        if (_weaponManager.CurrentWeapon != WeaponType.Dagger) return;
        if (_daggerCombat.IsParrying || !_daggerCombat.IsParryReady) return;

        _mover.StopHorizontal();
        Block(MobilityBlockReason.Parry);
        _state = PlayerState.Attack;

        _daggerCombat.RequestParry();
        _anim.PlayDaggerParry();
    }

    void OnParryFinished(ParryFinished _)
    {
        _anim.StopDaggerParry();
        Unblock(MobilityBlockReason.Parry);
        if (_state == PlayerState.Attack)
            _state = Mathf.Abs(_moveX) > 0.01f ? PlayerState.Move : PlayerState.Idle;
    }

    public void ChargedAttack()
    {
        CancelCharging();

        if (_state == PlayerState.Dead) return;
        if (_state is PlayerState.Heal or PlayerState.Hurt
            or PlayerState.BonfireRest or PlayerState.ChangeWeapon or PlayerState.Grapple) return;

        switch (_weaponManager.CurrentWeapon)
        {
            case WeaponType.Dagger:
                if (!_daggerCombat.IsChargedReady) return;
                _mover.StopHorizontal();
                Block(MobilityBlockReason.Attack);
                _daggerCombat.RequestChargedAttack();
                _state = PlayerState.Attack;
                break;

            case WeaponType.Sword:
                if (_jumper.IsGrounded) _mover.StopHorizontal();
                Block(MobilityBlockReason.Attack);
                _swordCombat.RequestChargedAttack(_jumper.IsGrounded);
                _state = PlayerState.Attack;
                break;
        }
    }

    public void ChargeBegin()
    {
        if (_state == PlayerState.Dead) return;
        if (_state is PlayerState.Heal or PlayerState.Hurt or PlayerState.Attack
            or PlayerState.BonfireRest or PlayerState.ChangeWeapon or PlayerState.Grapple or PlayerState.Dash) return;

        _isCharging = true;

        if (_weaponManager.CurrentWeapon == WeaponType.Sword)
        {
            if (_jumper.IsGrounded) _mover.StopHorizontal();
            Block(MobilityBlockReason.Charge);
        }
    }

    public void ChargeCancelled()
    {
        CancelCharging();
    }

    void CancelCharging()
    {
        if (!_isCharging) return;
        _isCharging = false;
        Unblock(MobilityBlockReason.Charge);
    }

    public void SwitchWeapon()
    {
        if (_state is PlayerState.Dead or PlayerState.Hurt or PlayerState.Heal
            or PlayerState.Attack or PlayerState.Dash or PlayerState.Grapple
            or PlayerState.BonfireRest or PlayerState.ChangeWeapon)
            return;

        _state = PlayerState.ChangeWeapon;
        _mover.StopHorizontal();
        Block(MobilityBlockReason.WeaponSwitch);

        _anim.PlayChangeWeapon(() =>
        {
            _weaponManager.SwitchWeapon();
            Unblock(MobilityBlockReason.WeaponSwitch);
            _state = Mathf.Abs(_moveX) > 0.01f ? PlayerState.Move : PlayerState.Idle;
        });
    }

    public void Dash()
    {
        if (_state is PlayerState.Heal or PlayerState.Dead or PlayerState.BonfireRest
            or PlayerState.Hurt or PlayerState.Attack
            or PlayerState.Dash or PlayerState.Grapple or PlayerState.ChangeWeapon) return;
        
        if (_abilities != null && !_abilities.HasDash)  return;
        
        float dir = Mathf.Abs(_moveX) > 0.01f
            ? Mathf.Sign(_moveX)
            : 0f;                       

        bool grounded = _jumper.IsGrounded;

        if (_dasher.RequestDash(dir, grounded))
        {
            _mover.SetInput(0f);       
            _state = PlayerState.Dash;
        }
    }

    public void Grapple()
    {
        _bus.Fire(new PlayerGrappleRequested());
    }
    
    public void Tick()
    {
        if (_state == PlayerState.Hurt &&
            !_knock.IsInHitStun)            
        {
            Unblock(MobilityBlockReason.Hurt);
            _mover.StopHorizontal();         
            _state = Mathf.Abs(_moveX) > 0.01f ? PlayerState.Move : PlayerState.Idle;
        }
    }

    // ───────────────────── Signals ─────────────────────

    void OnAttackStarted(AttackStarted s)
    {
        _state = PlayerState.Attack;
        _activeAttack = s.mode;

        switch (s.mode)
        {
            case AttackMode.Combo:
                _anim.PlayAttack(s.index);
                break;
            case AttackMode.Up:
                _anim.PlayUpAttack();
                break;
            case AttackMode.AirUp:
                _anim.PlayAirUpAttack();
                break;
            case AttackMode.AirDown:
                _anim.PlayAirDownAttack();
                break;
            case AttackMode.AirFwd:
                _anim.PlayAirForwardAttack();
                break;
            case AttackMode.DaggerCombo:
                _anim.PlayDaggerAttack(s.index);
                break;
            case AttackMode.DaggerSuper:
                Block(MobilityBlockReason.Attack);
                _anim.PlayDaggerSuperAttack();
                break;
            case AttackMode.DaggerFlyUp:
                _anim.PlayDaggerFlyAttackUp();
                break;
            case AttackMode.DaggerFlyDown:
                _anim.PlayDaggerFlyAttackDown();
                break;
            case AttackMode.SwordCombo:
                _anim.PlaySwordAttack(s.index);
                break;
            case AttackMode.SwordAirFwd:
                _anim.PlaySwordAirForwardAttack();
                break;
            case AttackMode.SwordAirDown:
                _anim.PlaySwordAirDownAttack();
                break;
            case AttackMode.SwordAirUp:
                _anim.PlaySwordAirUpAttack();
                break;
            case AttackMode.SwordDashAttack:
                _mover.StopHorizontal();
                Block(MobilityBlockReason.Attack);
                _anim.PlaySwordDashAttack();
                break;
            case AttackMode.SwordSuper:
                _anim.PlaySwordSuperAttack();
                break;
            case AttackMode.SwordSuperAir:
                _anim.PlaySwordSuperAirAttack();
                break;
        }
    }

    void OnAttackFinished(AttackFinished s)
    {
        _activeAttack = null;
        Unblock(MobilityBlockReason.Attack);

        if (_state == PlayerState.Hurt || _state == PlayerState.Dead)
            return;

        if (_state == PlayerState.Dash)
            return;

        _state = Mathf.Abs(_moveX) > 0.01f ? PlayerState.Move : PlayerState.Idle;
    }

    void OnHealStarted(HealStarted _)
    {
        _state = PlayerState.Heal;
        _mover.StopHorizontal(); // Immediately stop moving when healing starts
        _anim.PlayHealStart();
    }

    void OnHealFinished(HealFinished _)
    {
        _state = PlayerState.Idle;
        _anim.PlayHealEnd(false, () => Unblock(MobilityBlockReason.Heal));
    }

    void OnHealInterrupted(HealInterrupted _)
    {
        if (_state != PlayerState.Hurt)
            _state = PlayerState.Idle;
        _anim.PlayHealEnd(true, () => Unblock(MobilityBlockReason.Heal));
    }

    void OnTookDamage(TookDamage _)
    {
        if (_state != PlayerState.Dead)
            EnterHurt();
    }

    void OnDied(Died _)
    {
        CancelCharging();
        _state = PlayerState.Dead;
        _anim.PlayDeath();
        _gate.BlockMovement(MobilityBlockReason.Hurt);
        _gate.BlockJump(MobilityBlockReason.Hurt);
    }

    void OnGroundedChanged(GroundedChanged g)
    {
        if (!g.grounded) return;

        if (_activeAttack == AttackMode.AirFwd ||
            _activeAttack == AttackMode.AirDown ||
            _activeAttack == AttackMode.AirUp ||
            _activeAttack == AttackMode.DaggerFlyUp ||
            _activeAttack == AttackMode.DaggerFlyDown ||
            _activeAttack == AttackMode.SwordAirFwd ||
            _activeAttack == AttackMode.SwordAirDown ||
            _activeAttack == AttackMode.SwordAirUp)
        {
            _bus.Fire(new AttackFinished { mode = _activeAttack.Value, index = 0 });
        }
    }

    void OnDashStarted(DashStarted s)
    {
        _state = PlayerState.Dash;
    }

    void OnDashFinished(DashFinished s)
    {
        if (_state == PlayerState.Dead || _state == PlayerState.Hurt)
            return;

        if (_state == PlayerState.Attack)
            return;

        if (_jumper.IsGrounded)
            _state = Mathf.Abs(_moveX) > 0.01f ? PlayerState.Move : PlayerState.Idle;
        else
            _state = PlayerState.Jump;
    }
    void OnGrappleRequested(PlayerGrappleRequested _)
    {
        if (_state == PlayerState.Dead ||
            _state == PlayerState.Hurt ||
            _state == PlayerState.Heal ||
            _state == PlayerState.Dash ||
            _state == PlayerState.Attack ||
            _state == PlayerState.BonfireRest)
            return;

        if (_grappler.IsGrappling)
            return;

        // stop input driving the mover; grappler will handle movement
        _mover.SetInput(0f);

        _state = PlayerState.Grapple;

        // High-level command to Grappler2D (which will auto-target)
        _bus.Fire(new GrappleCommand());
    }
    
    void OnGrappleFinished(GrappleFinished s)
    {
        if (_state == PlayerState.Dead || _state == PlayerState.Hurt)
            return;

        // If we were in Grapple state, move back to normal locomotion
        if (_jumper.IsGrounded)
            _state = Mathf.Abs(_moveX) > 0.01f ? PlayerState.Move : PlayerState.Idle;
        else
            _state = PlayerState.Jump;
    }
    
    void OnBonfireRestStateChanged(BonfireRestStateChanged s)
    {
        if (s.IsResting)
        {
            CancelCharging();

            _state = PlayerState.BonfireRest;

            _mover.SetInput(0f);
            _mover.StopHorizontal();

            if (_healer != null && _healer.IsHealing)
                _healer.CancelHealing();

            _combo?.Interrupt();
            _swordCombat?.Interrupt();
            _daggerCombat?.Interrupt();

            _gate.BlockMovement(MobilityBlockReason.Bonfire);
            _gate.BlockJump(MobilityBlockReason.Bonfire);

            // restore resources on ENTER
            _health.Heal(999);                 // full heal (safe)
            _healer.RestoreChargesToMax();     // you already have this method
        }
        else
        {
            // unlock
            _gate.UnblockMovement(MobilityBlockReason.Bonfire);
            _gate.UnblockJump(MobilityBlockReason.Bonfire);

            if (_state == PlayerState.BonfireRest)
                _state = PlayerState.Idle;
        }
    }

    // ───────────────────── Local ─────────────────────

    void EnterHurt()
    {
        if (_state == PlayerState.Dead)
            return;

        CancelCharging();

        if (_healer != null && _healer.IsHealing)
            _healer.CancelHealing();

        Block(MobilityBlockReason.Hurt);
        _state = PlayerState.Hurt;
        _anim.PlayHurt();
        _combo.Interrupt();
        _swordCombat.Interrupt();
        _daggerCombat.Interrupt();
    }

    void Block(MobilityBlockReason r)
    {
        _gate.BlockMovement(r);
        _gate.BlockJump(r);
    }

    void Unblock(MobilityBlockReason r)
    {
        _gate.UnblockMovement(r);
        _gate.UnblockJump(r);
    }

}
