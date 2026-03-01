using UnityEngine;

[CreateAssetMenu(menuName = "Configs/SwordAttackConfig")]
public class SwordAttackConfig : ScriptableObject
{
    public float damage = 15f;
    public float[] damages = { 12f, 15f, 25f };
}
