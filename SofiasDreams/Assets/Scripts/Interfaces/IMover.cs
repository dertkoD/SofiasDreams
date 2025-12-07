using UnityEngine;

public interface IMover
{
    void SetInput(float x);          // -1..1
    void SetMovementLocked(bool v);  // заблокировать/разблокировать ходьбу
    
    void SetExternalVelocity(Vector2 velocity, float hardLockDuration, float softCarryDuration, bool overrideX, bool overrideY);

    bool IsMovementLocked { get; }
    
    // used by Grappler2D (and anyone else) to force sprite facing
    void ForceFacing(int dir);   // dir < 0 => left, dir > 0 => right
    int FacingDir { get; }       // -1 or +1
}
