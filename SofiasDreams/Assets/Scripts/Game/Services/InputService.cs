using UnityEngine;

public class InputService : IInputService
{
    public float GetMoveAxis()   => Input.GetAxisRaw("Horizontal");
    public float GetVerticalRaw() => Input.GetAxisRaw("Vertical");
    
    public bool JumpPressed()    => Input.GetButtonDown("Jump");
    public bool JumpHeld()        => Input.GetButton("Jump");
    public bool JumpReleased()    => Input.GetButtonUp("Jump");
    
    public bool AttackPressed()  => Input.GetButtonDown("Fire1");
    public bool AttackHeld()      => Input.GetButton("Fire1");
    public bool HealPressed()    => Input.GetKeyDown(KeyCode.Q);
    public bool HealReleased()   => Input.GetKeyUp(KeyCode.Q);
    public bool  DashPressed()    => Input.GetKeyDown(KeyCode.LeftShift);

    public bool GrapplePressed() => Input.GetKeyDown(KeyCode.E);
}
