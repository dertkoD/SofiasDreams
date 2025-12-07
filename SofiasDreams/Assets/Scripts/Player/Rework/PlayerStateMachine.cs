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
    readonly Healer         _healer;
    readonly Health         _health;
    readonly Knockback2D    _knock;
    readonly IPlayerAnimator _anim;
    readonly Dasher2D       _dasher;
    readonly Grappler2D   _grappler;

    // SO
    readonly PlayerMovementConfig _moveSO;
    readonly PlayerJumpConfig     _jumpSO;
    readonly PlayerAttackConfig   _atkSO;
    readonly PlayerHealConfig     _healSO;
    readonly PlayerHealthConfig   _hpSO;
    readonly PlayerDashConfig     _dashSO;
    readonly PlayerGrappleConfig  _grappleSO;
    readonly HitReactionConfig _hitSO;

    float       _moveX;
    AttackMode? _activeAttack;

    public PlayerStateMachine(
        SignalBus bus, IMobilityGate gate,
        Mover2D mover, Jumper2D jumper,
        ICombat combo,
        Healer healer, Health health, Knockback2D knock, IPlayerAnimator anim,
        Dasher2D dasher, Grappler2D grappler,                                      
        [Inject(Optional = true)] PlayerMovementConfig moveSO,
        [Inject(Optional = true)] PlayerJumpConfig     jumpSO,
        [Inject(Optional = true)] PlayerAttackConfig   atkSO,
        [Inject(Optional = true)] PlayerHealConfig     healSO,
        [Inject(Optional = true)] PlayerDashConfig     dashSO, 
        [Inject(Optional = true)] PlayerGrappleConfig  grappleSO,
        PlayerHealthConfig hpSO,
        [Inject(Optional = true)] HitReactionConfig hitSO)
    {
        _bus      = bus;
        _gate     = gate;
        _mover    = mover;
        _jumper   = jumper;
        _combo    = combo;
        _healer   = healer;
        _health   = health;
        _knock    = knock;
        _anim     = anim;
        _dasher   = dasher;
        _grappler = grappler;

        _moveSO    = moveSO;
        _jumpSO    = jumpSO;
        _atkSO     = atkSO;
        _healSO    = healSO;
        _dashSO    = dashSO;
        _hpSO      = hpSO;
        _grappleSO = grappleSO;
        _hitSO     = hitSO;
    }

    public void Initialize()
    {
        if (_moveSO != null)
            _mover.Configure(new MoveSettings { moveSpeed = _moveSO.moveSpeed });

        if (_jumpSO != null)
            _jumper.Configure(new JumpSettings {
                jumpVelocity   = _jumpSO.jumpForce,
                coyoteTime     = _jumpSO.coyoteTime,
                jumpBufferTime = _jumpSO.jumpBufferTime,
                dropDuration   = _jumpSO.dropDuration,
                jumpCutMultiplier = _jumpSO.jumpCutMultiplier
            });
        _jumper.Inject(_gate, _bus);

        if (_atkSO != null && _combo is Combat3 c3)
            c3.Configure(BuildAttackSettings(_atkSO));

        if (_healSO != null)
        {
            _healer.Configure(
                new HealSettings {
                    amount = _healSO.healAmount
                },
                _healSO.maxCharges,
                _healSO.killsPerCharge
            );
        }

        if (_dashSO != null)
        {
            _dasher.Configure(new DashSettings {
                dashSpeed    = _dashSO.dashSpeed,
                cooldown     = _dashSO.cooldown,
                allowAirDash = _dashSO.allowAirDash,
                accel        = _dashSO.accel,
                decel        = _dashSO.decel
            });
        }

        if (_grappleSO != null)
        {
            _grappler.Configure(new GrappleSettings
            {
                radius                    = _grappleSO.radius,
                grappleLayer              = _grappleSO.grappleLayer,
                obstacleLayer             = _grappleSO.obstacleLayer,
                moveSpeed                 = _grappleSO.moveSpeed,
                stopDistance              = _grappleSO.stopDistance,
                arrivalClearance          = _grappleSO.arrivalClearance,
                zeroGravityWhileGrappling = _grappleSO.zeroGravityWhileGrappling,
                startupDelay              = _grappleSO.startupDelay,
                exitStrength              = _grappleSO.exitStrength,
                carryOverEntrySpeedFactor = _grappleSO.carryOverEntrySpeedFactor,
                maxExitSpeedX             = _grappleSO.maxExitSpeedX,
                maxExitSpeedY             = _grappleSO.maxExitSpeedY,
                exitBlendTime             = _grappleSO.exitBlendTime,
                blendByVelocityLerp       = _grappleSO.blendByVelocityLerp,
                hardLockDuration          = _grappleSO.hardLockDuration,
                softCarryMaxDuration      = _grappleSO.softCarryMaxDuration,
                cooldown                  = _grappleSO.cooldown
            });
        }

        _health.Configure(new HealthSettings {
            maxHP     = _hpSO.maxHP,
            invulnTime = _hpSO.invulnTime
        });
        
        _knock.Configure(new KnockbackSettings {
            defaultHitStop = _hitSO != null ? _hitSO.hitStun : 0.05f
        });
        
        _health.Inject(_bus);

        _knock.Configure(new KnockbackSettings { defaultHitStop = 0.05f });

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
    }

    // ───────────────────── Commands ─────────────────────

    public void Move(float x)
    {
        if (_state == PlayerState.Dead || _state == PlayerState.Dash) 
            return;
        
        _moveX = x;
        _mover.SetInput(x);

        if (_gate.IsMovementBlocked) return;
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
            _state == PlayerState.Attack)
            return;
        
        if (_gate.IsJumpBlocked) return;

        _jumper.RequestJump();
        _state = PlayerState.Jump;
        _anim.SetGrounded(false);
    }
    
    public void JumpRelease()
    {
        if (_state == PlayerState.Dead)
            return;

        if (_gate.IsJumpBlocked)
            return;

        _jumper.NotifyJumpReleased();
    }

    public void Attack()
    {
        if (_state == PlayerState.Dead) return;
        if (_state is PlayerState.Heal or PlayerState.Hurt) return;
        
        if (_jumper.IsGrounded)
        {
            _mover.StopHorizontal(); 
        }

        Block(MobilityBlockReason.Attack);
        _combo.RequestAttack();
        _state = PlayerState.Attack;
    }

    public void UpAttack()
    {
        if (_state == PlayerState.Dead) return;
        if (_state is PlayerState.Heal or PlayerState.Hurt) return;

        Block(MobilityBlockReason.Attack);

        if (_jumper.IsGrounded)
        {
            _anim.PlayUpAttack();
            _bus.Fire(new AttackStarted { mode = AttackMode.Up, index = 0 });
        }
        else
        {
            _anim.PlayAirUpAttack();
            _bus.Fire(new AttackStarted { mode = AttackMode.AirUp, index = 0 });
        }

        _state = PlayerState.Attack;
    }

    public void ForwardJumpAttack()
    {
        if (_state == PlayerState.Dead || _jumper.IsGrounded) return;
        if (_state is PlayerState.Heal or PlayerState.Hurt) return;

        _anim.PlayAirForwardAttack();
        _bus.Fire(new AttackStarted { mode = AttackMode.AirFwd, index = 0 });
        _state = PlayerState.Attack;
    }

    public void UpJumpAttack()
    {
        if (_state == PlayerState.Dead || _jumper.IsGrounded) return;
        if (_state is PlayerState.Heal or PlayerState.Hurt) return;

        _anim.PlayAirUpAttack();
        _bus.Fire(new AttackStarted { mode = AttackMode.AirUp, index = 0 });
        _state = PlayerState.Attack;
    }

    public void DownJumpAttack()
    {
        if (_state == PlayerState.Dead || _jumper.IsGrounded) return;
        if (_state is PlayerState.Heal or PlayerState.Hurt) return;

        _anim.PlayAirDownAttack();
        _bus.Fire(new AttackStarted { mode = AttackMode.AirDown, index = 0 });
        _state = PlayerState.Attack;
    }

    public void HealBegin()
    {
        if (_state == PlayerState.Dead || _state == PlayerState.Hurt)
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
            _state == PlayerState.Grapple)
            return;

        if (!_jumper.IsGrounded)
            return;

        _jumper.RequestDropThrough();

        // Treat as airborne for animation / logic
        _state = PlayerState.Jump;
        _anim.SetGrounded(false);
    }

    public void ApplyDamage(DamageInfo info)
    {
        if (_state == PlayerState.Dead)
            return;

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

    public void Dash()
    {
        if (_state == PlayerState.Dead) return;
        if (_state is PlayerState.Heal or PlayerState.Hurt or PlayerState.Attack or PlayerState.Dash)
            return;

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
            !_knock.IsInHitStun &&          
            !_health.IsInvincible)            
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

        if (s.mode == AttackMode.Combo)
            _anim.PlayAttack(s.index);
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
        _anim.PlayHealStart();
    }

    void OnHealFinished(HealFinished _)
    {
        Unblock(MobilityBlockReason.Heal);
        _state = PlayerState.Idle;
        _anim.PlayHealEnd(false);
    }

    void OnHealInterrupted(HealInterrupted _)
    {
        Unblock(MobilityBlockReason.Heal);
        if (_state != PlayerState.Hurt)
            _state = PlayerState.Idle;
        _anim.PlayHealEnd(true);
    }

    void OnTookDamage(TookDamage _)
    {
        if (_state != PlayerState.Dead)
            EnterHurt();
    }

    void OnDied(Died _)
    {
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
            _activeAttack == AttackMode.AirUp)
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
            _state == PlayerState.Attack)
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

    // ───────────────────── Local ─────────────────────

    void EnterHurt()
    {
        if (_state == PlayerState.Dead)
            return;

        if (_healer != null && _healer.IsHealing)
            _healer.CancelHealing();

        Block(MobilityBlockReason.Hurt);
        _state = PlayerState.Hurt;
        _anim.PlayHurt();
        _combo.Interrupt();
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

    static AttackSettings BuildAttackSettings(PlayerAttackConfig so)
    {
        float d1 = (so.damages != null && so.damages.Length > 0) ? so.damages[0] : so.damage;
        float d2 = (so.damages != null && so.damages.Length > 1) ? so.damages[1] : so.damage;
        float d3 = (so.damages != null && so.damages.Length > 2) ? so.damages[2] : so.damage;

        return new AttackSettings {
            a1 = new AttackStep { damage = d1 },
            a2 = new AttackStep { damage = d2 },
            a3 = new AttackStep { damage = d3 },
        };
    }
}
