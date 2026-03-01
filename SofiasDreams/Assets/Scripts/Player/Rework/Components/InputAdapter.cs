using System;
using UnityEngine;
using Zenject;

public class InputAdapter : MonoBehaviour, IInitializable, IDisposable
{
    [SerializeField] float DeadZone = 0.1f;
    [SerializeField] float upAttackBuffer = 0.08f;
    [SerializeField] float chargeHoldTime = 0.5f;

    IInputService _input;
    IPlayerCommands _commands;
    SignalBus _bus;
    bool _isGrounded = true;
    bool _isDashing;
    bool _attackDownLastFrame;
    float _pendingGroundAttackTimer;
    float _attackHoldTime;

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
    }

    public void Dispose()
    {
        _bus.TryUnsubscribe<GroundedChanged>(OnGroundedChanged);
        _bus.TryUnsubscribe<DashStarted>(OnDashStarted);
        _bus.TryUnsubscribe<DashFinished>(OnDashFinished);
    }

    void OnGroundedChanged(GroundedChanged g) => _isGrounded = g.grounded;
    void OnDashStarted(DashStarted _) => _isDashing = true;
    void OnDashFinished(DashFinished _) => _isDashing = false;
    
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

        if (attackDown)
            _attackHoldTime += Time.deltaTime;

        if (attackReleasedThisFrame)
        {
            if (_attackHoldTime >= chargeHoldTime)
                _commands.ChargedAttack();
            _attackHoldTime = 0f;
        }

        float y = _input.GetVerticalRaw();

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

        if (_isGrounded)
        {
            if (attackPressedThisFrame)
            {
                if (_isDashing)
                    _commands.Attack();
                else if (y > 0f)
                    _commands.UpAttack();
                else
                    _pendingGroundAttackTimer = upAttackBuffer;
            }
        }
        else
        {
            if (attackPressedThisFrame)
            {
                if (y > 0f)
                    _commands.UpJumpAttack();
                else if (y < 0f)
                    _commands.DownJumpAttack();
                else
                    _commands.ForwardJumpAttack();
            }
        }
    }
}
