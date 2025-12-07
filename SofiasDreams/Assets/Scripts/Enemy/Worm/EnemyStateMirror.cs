using UnityEngine;

public class EnemyStateMirror : MonoBehaviour
{
    public enum Phase { Patrol, Windup, Spin, Stun }
    public Phase LastPhase { get; private set; } = Phase.Patrol;

    public void SetPhase(Phase p) => LastPhase = p;
}
