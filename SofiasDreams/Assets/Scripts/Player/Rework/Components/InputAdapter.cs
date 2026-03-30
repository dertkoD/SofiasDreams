using System;
using UnityEngine;
using Zenject;

public class InputAdapter : MonoBehaviour, IInitializable, IDisposable
{
    [SerializeField] float DeadZone = 0.1f;
    [SerializeField] float upAttackBuffer = 0.08f;
    [SerializeField] float chargeHoldTime = 0.5f;
    [Tooltip("How long to wait after press before deciding tap vs hold")]
    [SerializeField] float chargeDecisionTime = 0.15f;

    IInputService _input;
    IPlayerCommands _commands;
    SignalBus _bus;
    bool _isGrounded = true;
    bool _isDashing;
    bool _attackDownLastFrame;
    float _pendingGroundAttackTimer;

    enum AttackPhase { None, Pending, Charging }
    AttackPhase _phase;
    float _holdTimer;
    float _pendingUpY;

    [Inject]
    public void Construct(IInputService input, IPlayerCommands commands, SignalBus bus)
    {
        _input = input;
        _commands = commands;
        _bus = bus;
    }

    public void Initialize()
    {
        _bus.Subscribe<GroundedChanged>(OnGroundedChanged);
        _bus.Subscribe<DashStarted>(OnDashStarted);
        _bus.Subscribe<DashFinished>(OnDashFinished);
        _bus.Subscribe<TookDamage>(OnTookDamage);
    }

    public void Dispose()
    {
        _bus.TryUnsubscribe<GroundedChanged>(OnGroundedChanged);
        _bus.TryUnsubscribe<DashStarted>(OnDashStarted);
        _bus.TryUnsubscribe<DashFinished>(OnDashFinished);
        _bus.TryUnsubscribe<TookDamage>(OnTookDamage);
    }

    void OnGroundedChanged(GroundedChanged g) => _isGrounded = g.grounded;
    void OnDashStarted(DashStarted _) => _isDashing = true;
    void OnDashFinished(DashFinished _) => _isDashing = false;

    void OnTookDamage(TookDamage _)
    {
        if (_phase == AttackPhase.Charging)
            _commands.ChargeCancelled();
        _phase = AttackPhase.None;
        _holdTimer = 0f;
        _pendingGroundAttackTimer = 0f;
    }

    void Update()
    {
        float x = _input.GetMoveAxis();
        if (Mathf.Abs(x) > DeadZone) _commands.Move(x);
        else                          _commands.Stop();

        bool jumpPressedThisFrame  = _input.JumpPressed();
        bool jumpReleasedThisFrame = _input.JumpReleased(); 

        if (jumpPressedThisFrame) 
        { 
            float y = _input.GetVerticalRaw();

            if (y < 0f && _isGrounded)
                _commands.DropPlatform();
            else
                _commands.Jump();
        }

        if (jumpReleasedThisFrame)
        {
            _commands.JumpRelease();
        }

        if (_input.GrapplePressed()) _commands.Grapple();

        HandleAttack(jumpPressedThisFrame);

        if (_input.HealPressed())  _commands.HealBegin();
        if (_input.HealReleased()) _commands.HealCancel();

        if (_input.DashPressed())
            _commands.Dash();

        if (_input.InteractPressed())
            _commands.Interact();

        if (_input.WeaponSwitchPressed())
            _commands.SwitchWeapon();

        if (_input.ParryPressed())
            _commands.Parry();
    }

    void HandleAttack(bool jumpPressedThisFrame)
    {
        if (jumpPressedThisFrame)
            return;

        bool attackDown = _input.AttackHeld();
        bool attackPressedThisFrame = attackDown && !_attackDownLastFrame;
        bool attackReleasedThisFrame = !attackDown && _attackDownLastFrame;
        _attackDownLastFrame = attackDown;

        float y = _input.GetVerticalRaw();

        switch (_phase)
        {
            case AttackPhase.None:
                HandlePhaseNone(attackPressedThisFrame, y);
                break;

            case AttackPhase.Pending:
                HandlePhasePending(attackDown, attackReleasedThisFrame, y);
                break;

            case AttackPhase.Charging:
                HandlePhaseCharging(attackDown, attackReleasedThisFrame);
                break;
        }
    }

    void HandlePhaseNone(bool attackPressedThisFrame, float y)
    {
        if (_pendingGroundAttackTimer > 0f)
        {
            _pendingGroundAttackTimer -= Time.deltaTime;
            if (y > 0f)
            {
                _pendingGroundAttackTimer = 0f;
                _commands.UpAttack();
            }
            else if (_pendingGroundAttackTimer <= 0f)
            {
                _commands.Attack();
            }
            return;
        }

        if (!attackPressedThisFrame) return;

        if (!_isGrounded)
        {
            if (y > 0f)        _commands.UpJumpAttack();
            else if (y < 0f)   _commands.DownJumpAttack();
            else                _commands.ForwardJumpAttack();
            return;
        }

        if (_isDashing)
        {
            _commands.Attack();
            return;
        }

        if (y > 0f)
        {
            _commands.UpAttack();
            return;
        }

        _phase = AttackPhase.Pending;
        _holdTimer = 0f;
        _pendingUpY = y;
    }

    void HandlePhasePending(bool attackDown, bool attackReleasedThisFrame, float y)
    {
        _holdTimer += Time.deltaTime;

        if (attackReleasedThisFrame)
        {
            _phase = AttackPhase.None;
            _pendingGroundAttackTimer = upAttackBuffer;
            _pendingUpY = y;
            return;
        }

        if (_holdTimer >= chargeDecisionTime)
        {
            _phase = AttackPhase.Charging;
            _commands.ChargeBegin();
        }
    }

    void HandlePhaseCharging(bool attackDown, bool attackReleasedThisFrame)
    {
        _holdTimer += Time.deltaTime;

        if (!attackReleasedThisFrame) return;

        _phase = AttackPhase.None;

        if (_holdTimer >= chargeHoldTime)
            _commands.ChargedAttack();
        else
            _commands.ChargeCancelled();

        _holdTimer = 0f;
    }
}
