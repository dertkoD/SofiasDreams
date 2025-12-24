public sealed class PlayerAbilities : IPlayerAbilities
{
    public bool HasDash { get; private set; }

    public void GrantDash() => HasDash = true;
}
