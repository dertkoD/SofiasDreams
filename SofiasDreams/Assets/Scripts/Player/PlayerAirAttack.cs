using UnityEngine;

public class PlayerAirAttack : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerJump playerJump;
    [SerializeField] private PlayerRangedAbility powershot; // respect lock

    [SerializeField] private float airAttackDuration = 0.35f;
    private bool isAirAttacking = false;
    private float airAttackTimer;

    public bool IsAirAttacking => isAirAttacking;

    private static readonly int IsFlyingAttack = Animator.StringToHash("IsFlyingAttack");

    private void Update()
    {
        // Respect shot lock
        if (powershot && powershot.IsShotLocked)
        {
            if (isAirAttacking)
            {
                animator.SetBool(IsFlyingAttack, false);
                isAirAttacking = false;
            }
            return;
        }

        if (!isAirAttacking && !playerJump.IsGrounded && Input.GetMouseButtonDown(0))
        {
            isAirAttacking = true;
            airAttackTimer = airAttackDuration;

            animator.SetBool(IsFlyingAttack, true);
            animator.Play("FlyingAttack");
        }

        if (isAirAttacking)
        {
            airAttackTimer -= Time.deltaTime;
            if (airAttackTimer <= 0f || playerJump.IsGrounded)
            {
                animator.SetBool(IsFlyingAttack, false);
                isAirAttacking = false;
            }
        }
    }
}
