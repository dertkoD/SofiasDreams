public interface IJumper
{
    bool IsGrounded { get; }
    void RequestJump();
    bool RequestDropThrough();
    void NotifyJumpReleased();
}
