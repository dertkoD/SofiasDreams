using System;
using UnityEngine;
using Zenject;

public class InputAdapter : MonoBehaviour, IInitializable, IDisposable
{
    [SerializeField] float DeadZone = 0.1f;

    IInputService _input;
    IPlayerCommands _commands;
    SignalBus _bus;
    bool _isGrounded = true; 
    bool _attackDownLastFrame;

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
    }

    public void Dispose()
    {
        _bus.TryUnsubscribe<GroundedChanged>(OnGroundedChanged);
    }

    void OnGroundedChanged(GroundedChanged g) => _isGrounded = g.grounded;
    
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
        _attackDownLastFrame = _input.AttackHeld();

        if (_input.HealPressed())  _commands.HealBegin();
        if (_input.HealReleased()) _commands.HealCancel();
        
        if (_input.DashPressed())
            _commands.Dash();
    }

    void HandleAttack(bool jumpPressedThisFrame)
    {
        if (jumpPressedThisFrame)
            return;

        bool attackDown = _input.AttackHeld();
        bool attackPressedThisFrame = attackDown && !_attackDownLastFrame;

        float y = _input.GetVerticalRaw();

        if (_isGrounded)
        {
            if (attackPressedThisFrame)
            {
                if (y > 0f)
                    _commands.UpAttack();
                else
                    _commands.Attack();
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
