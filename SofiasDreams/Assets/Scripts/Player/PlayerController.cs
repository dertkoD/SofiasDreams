using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Player's Scripts")]
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private PlayerJump jump;
    [SerializeField] private PlayerAttack attack;
    [SerializeField] private PlayerHeal heal;
    [SerializeField] private PlayerRangedAbility powershot;
    [SerializeField] private GrappleSystem grapple;
    void Update()
    {
        if (heal != null && heal.IsHealing) return;
        if (powershot != null && powershot.IsShotLocked) return;

        movement.HandleMoveInput();
        jump.HandleJump();
        attack.HandleAttack();
        
        if (!heal.IsHealing && !powershot.IsShotLocked && !attack.IsAttacking) grapple.HandleGrappling();
    }
}
