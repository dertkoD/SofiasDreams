using UnityEngine;

[CreateAssetMenu(menuName="Configs/UpAttack")]
public class PlayerUpAttackConfig : ScriptableObject
{
    public float damage   = 15f;
    public float cooldown = 0.1f;
}
