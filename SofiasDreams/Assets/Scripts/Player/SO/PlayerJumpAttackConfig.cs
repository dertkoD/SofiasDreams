using UnityEngine;

[CreateAssetMenu(menuName="Configs/JumpAttack")]
public class PlayerJumpAttackConfig : ScriptableObject
{
    public float damage = 10f;
    public float cooldown = 0.2f;
}
