public interface  IInputService
{
    float GetMoveAxis();
    float GetVerticalRaw();
    bool JumpPressed();
    bool JumpHeld();
    bool JumpReleased();
    bool AttackPressed();
    bool AttackHeld();
    bool HealPressed();
    bool HealReleased();
    bool DashPressed();
    bool GrapplePressed();
}
