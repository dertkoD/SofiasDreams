using UnityEngine;

[CreateAssetMenu(menuName = "Configs/PlayerMovementConfig")]
public class PlayerMovementConfig : ScriptableObject
{
    public float moveSpeed = 8f;
    
    [Header("Horizontal Accel/Decel (seconds)")]
    [Tooltip("Time to go from 0 to max speed while the player is holding a direction.")]
    public float accelerationTime = 0.08f;

    [Tooltip("Time to go from current speed to 0 when the player releases horizontal input.")]
    public float decelerationTime = 0.10f;
}
