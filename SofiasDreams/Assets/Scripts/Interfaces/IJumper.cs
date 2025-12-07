public interface IJumper
{
    bool IsGrounded { get; }
    void RequestJump();
    void RequestDropThrough();
    void NotifyJumpReleased();
}
