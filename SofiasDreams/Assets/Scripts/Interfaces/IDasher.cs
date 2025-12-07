public interface IDasher
{
    bool IsDashing { get; }
    bool RequestDash(float direction, bool isGrounded);
}
