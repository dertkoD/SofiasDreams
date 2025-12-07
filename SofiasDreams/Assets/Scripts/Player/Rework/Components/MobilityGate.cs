using System.Collections.Generic;

public class MobilityGate : IMobilityGate
{
    readonly HashSet<MobilityBlockReason> _move = new();
    readonly HashSet<MobilityBlockReason> _jump = new();

    public void BlockMovement(MobilityBlockReason r)  => _move.Add(r);
    public void UnblockMovement(MobilityBlockReason r)=> _move.Remove(r);
    public void BlockJump(MobilityBlockReason r)      => _jump.Add(r);
    public void UnblockJump(MobilityBlockReason r)    => _jump.Remove(r);

    public bool IsMovementBlocked => _move.Count > 0;
    public bool IsJumpBlocked => _jump.Count > 0;
}
