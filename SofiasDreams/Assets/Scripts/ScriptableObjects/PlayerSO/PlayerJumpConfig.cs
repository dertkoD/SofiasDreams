using UnityEngine;

[CreateAssetMenu(menuName = "Configs/PlayerJumpConfig")]
public class PlayerJumpConfig : ScriptableObject
{
    public float jumpForce = 15f;
    public float coyoteTime = 0.1f;
    public float jumpBufferTime = 0.15f;
    public float groundCheckRadius = 0.71f;
    public float dropDuration;
    public float jumpCutMultiplier = 0.5f;
}
