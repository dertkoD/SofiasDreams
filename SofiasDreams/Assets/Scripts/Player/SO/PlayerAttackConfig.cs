using UnityEngine;

[CreateAssetMenu(menuName = "Configs/PlayerAttackConfig")]
public class PlayerAttackConfig : ScriptableObject
{
    public float damage = 10f;
    public float[] damages = { 10f, 10f, 20f };
}
