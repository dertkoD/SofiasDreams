using UnityEngine;

[CreateAssetMenu(menuName = "Configs/PlayerDash")]
public class PlayerDashConfig : ScriptableObject
{
    [Header("Base")]
    public float dashSpeed    = 15f;
    public float cooldown     = 0.5f;
    public bool  allowAirDash = true;

    [Header("Dash dynamics")]
    public float accel = 80f;
    public float decel = 80f;
}
