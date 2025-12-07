using UnityEngine;

[CreateAssetMenu(menuName = "Configs/PlayerHealthConfig")]
public class PlayerHealthConfig : ScriptableObject
{
    public int maxHP = 5;
    public float invulnTime = 0.3f;
}
