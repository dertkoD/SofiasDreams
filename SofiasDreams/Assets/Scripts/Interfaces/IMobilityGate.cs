public interface IMobilityGate
{
    void BlockMovement(MobilityBlockReason reason);
    void UnblockMovement(MobilityBlockReason reason);
    void BlockJump(MobilityBlockReason reason);
    void UnblockJump(MobilityBlockReason reason);

    bool IsMovementBlocked { get; }
    bool IsJumpBlocked { get; }
}
