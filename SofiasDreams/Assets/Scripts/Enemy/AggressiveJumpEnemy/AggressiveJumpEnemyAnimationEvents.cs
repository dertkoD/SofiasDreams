using UnityEngine;

/// <summary>
/// Bridges animation clip events to the jump motor so designers can invoke jumps from clips.
/// </summary>
public class AggressiveJumpEnemyAnimationEvents : MonoBehaviour
{
    [SerializeField] AggressiveJumpEnemyMotor _motor;

    void Awake()
    {
        if (!_motor)
            _motor = GetComponentInParent<AggressiveJumpEnemyMotor>();
    }

    public void PerformPatrolJump()
    {
        _motor?.PerformPatrolJump();
    }

    public void PerformAttackJump()
    {
        _motor?.PerformAttackJump();
    }
}
