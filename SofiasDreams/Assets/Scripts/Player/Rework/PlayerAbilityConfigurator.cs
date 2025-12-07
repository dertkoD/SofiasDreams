using Zenject;

public interface IPlayerAbilityConfigurator
{
    void Configure();
}

public sealed class PlayerAbilityConfigurator : IPlayerAbilityConfigurator
{
    bool _configured;

    readonly SignalBus _bus;
    readonly IMobilityGate _gate;
    readonly Mover2D _mover;
    readonly Jumper2D _jumper;
    readonly ICombat _combat;
    readonly Healer _healer;
    readonly Health _health;
    readonly Knockback2D _knockback;
    readonly Dasher2D _dasher;
    readonly Grappler2D _grappler;

    readonly PlayerMovementConfig _movementConfig;
    readonly PlayerJumpConfig _jumpConfig;
    readonly PlayerAttackConfig _attackConfig;
    readonly PlayerHealConfig _healConfig;
    readonly PlayerHealthConfig _healthConfig;
    readonly PlayerDashConfig _dashConfig;
    readonly PlayerGrappleConfig _grappleConfig;
    readonly HitReactionConfig _hitReactionConfig;

    public PlayerAbilityConfigurator(
        SignalBus bus,
        IMobilityGate gate,
        Mover2D mover,
        Jumper2D jumper,
        ICombat combat,
        Healer healer,
        Health health,
        Knockback2D knockback,
        Dasher2D dasher,
        Grappler2D grappler,
        [Inject(Optional = true)] PlayerMovementConfig movementConfig,
        [Inject(Optional = true)] PlayerJumpConfig jumpConfig,
        [Inject(Optional = true)] PlayerAttackConfig attackConfig,
        [Inject(Optional = true)] PlayerHealConfig healConfig,
        PlayerHealthConfig healthConfig,
        [Inject(Optional = true)] PlayerDashConfig dashConfig,
        [Inject(Optional = true)] PlayerGrappleConfig grappleConfig,
        [Inject(Optional = true)] HitReactionConfig hitReactionConfig)
    {
        _bus = bus;
        _gate = gate;
        _mover = mover;
        _jumper = jumper;
        _combat = combat;
        _healer = healer;
        _health = health;
        _knockback = knockback;
        _dasher = dasher;
        _grappler = grappler;
        _movementConfig = movementConfig;
        _jumpConfig = jumpConfig;
        _attackConfig = attackConfig;
        _healConfig = healConfig;
        _healthConfig = healthConfig;
        _dashConfig = dashConfig;
        _grappleConfig = grappleConfig;
        _hitReactionConfig = hitReactionConfig;
    }

    public void Configure()
    {
        if (_configured)
            return;

        ConfigureMovement();
        ConfigureJump();
        ConfigureCombat();
        ConfigureHeal();
        ConfigureDash();
        ConfigureGrapple();
        ConfigureHealth();
        ConfigureKnockback();

        _configured = true;
    }

    void ConfigureMovement()
    {
        if (_movementConfig == null)
            return;

        _mover.Configure(new MoveSettings { moveSpeed = _movementConfig.moveSpeed });
    }

    void ConfigureJump()
    {
        if (_jumpConfig != null)
        {
            _jumper.Configure(new JumpSettings
            {
                jumpVelocity = _jumpConfig.jumpForce,
                coyoteTime = _jumpConfig.coyoteTime,
                jumpBufferTime = _jumpConfig.jumpBufferTime,
                dropDuration = _jumpConfig.dropDuration,
                jumpCutMultiplier = _jumpConfig.jumpCutMultiplier
            });
        }

        _jumper.Inject(_gate, _bus);
    }

    void ConfigureCombat()
    {
        if (_attackConfig == null)
            return;

        if (_combat is Combat3 combo)
        {
            combo.Configure(BuildAttackSettings(_attackConfig));
        }
    }

    void ConfigureHeal()
    {
        if (_healConfig == null)
            return;

        _healer.Configure(
            new HealSettings { amount = _healConfig.healAmount },
            _healConfig.maxCharges,
            _healConfig.killsPerCharge);
    }

    void ConfigureDash()
    {
        if (_dashConfig == null)
            return;

        _dasher.Configure(new DashSettings
        {
            dashSpeed = _dashConfig.dashSpeed,
            cooldown = _dashConfig.cooldown,
            allowAirDash = _dashConfig.allowAirDash,
            accel = _dashConfig.accel,
            decel = _dashConfig.decel
        });
    }

    void ConfigureGrapple()
    {
        if (_grappleConfig == null)
            return;

        _grappler.Configure(new GrappleSettings
        {
            radius = _grappleConfig.radius,
            grappleLayer = _grappleConfig.grappleLayer,
            obstacleLayer = _grappleConfig.obstacleLayer,
            moveSpeed = _grappleConfig.moveSpeed,
            stopDistance = _grappleConfig.stopDistance,
            arrivalClearance = _grappleConfig.arrivalClearance,
            zeroGravityWhileGrappling = _grappleConfig.zeroGravityWhileGrappling,
            startupDelay = _grappleConfig.startupDelay,
            exitStrength = _grappleConfig.exitStrength,
            carryOverEntrySpeedFactor = _grappleConfig.carryOverEntrySpeedFactor,
            maxExitSpeedX = _grappleConfig.maxExitSpeedX,
            maxExitSpeedY = _grappleConfig.maxExitSpeedY,
            exitBlendTime = _grappleConfig.exitBlendTime,
            blendByVelocityLerp = _grappleConfig.blendByVelocityLerp,
            hardLockDuration = _grappleConfig.hardLockDuration,
            softCarryMaxDuration = _grappleConfig.softCarryMaxDuration,
            cooldown = _grappleConfig.cooldown
        });
    }

    void ConfigureHealth()
    {
        if (_healthConfig != null)
        {
            _health.Configure(new HealthSettings
            {
                maxHP = _healthConfig.maxHP,
                invulnTime = _healthConfig.invulnTime
            });
        }

        _health.Inject(_bus);
    }

    void ConfigureKnockback()
    {
        float hitStop = _hitReactionConfig != null ? _hitReactionConfig.hitStun : 0.05f;
        _knockback.Configure(new KnockbackSettings { defaultHitStop = hitStop });
    }

    static AttackSettings BuildAttackSettings(PlayerAttackConfig config)
    {
        float d1 = (config.damages != null && config.damages.Length > 0) ? config.damages[0] : config.damage;
        float d2 = (config.damages != null && config.damages.Length > 1) ? config.damages[1] : config.damage;
        float d3 = (config.damages != null && config.damages.Length > 2) ? config.damages[2] : config.damage;

        return new AttackSettings
        {
            a1 = new AttackStep { damage = d1 },
            a2 = new AttackStep { damage = d2 },
            a3 = new AttackStep { damage = d3 }
        };
    }
}
