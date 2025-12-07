using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [SerializeField] private GameObject weaponObject;
    [SerializeField] private Animator animator;
    public bool IsAttacking { get; private set; }

    private int comboCount = 0;
    private bool nextAttackQueued = false;

    public void HandleAttack()
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (Input.GetMouseButtonDown(0) && comboCount < 3 && stateInfo.IsName("Movement"))
        {
            PerformAttack();
        }
        else if (Input.GetMouseButtonDown(0) && comboCount > 0 && comboCount < 3)
        {
            nextAttackQueued = true;
        }
    }

    void PerformAttack()
    {
        if (comboCount >= 3) return;

        comboCount++;
        IsAttacking = true;

        switch (comboCount)
        {
            case 1:
                animator.SetBool("IsAttacking1", true);
                break;
            case 2:
                animator.SetBool("IsAttacking2", true);
                break;
            case 3:
                animator.SetBool("IsAttacking3", true);
                break;
        }
        Debug.Log("Performing attack, comboCount: " + comboCount);
        nextAttackQueued = false;
    }

    public void OnAttackAnimationEnd()
    {
        IsAttacking = false;
        
        if (nextAttackQueued && comboCount < 3)
        {
            if (nextAttackQueued && comboCount < 3)
            {
                PerformAttack();
                nextAttackQueued = false;
                Debug.Log("Queued attack processed, comboCount: " + comboCount);
            }
        }
        else
        {
            ResetCombo();
            Debug.Log("Combo reset after animation end");
        }
    }

    public void InterruptAttack()
    {
        IsAttacking = false;
        nextAttackQueued = false;
        comboCount = 0;

        if (animator)
        {
            animator.SetBool("IsAttacking1", false);
            animator.SetBool("IsAttacking2", false);
            animator.SetBool("IsAttacking3", false);
        }
    }
    
    void ResetCombo()
    {
        comboCount = 0;
        animator.SetBool("IsAttacking1", false);
        animator.SetBool("IsAttacking2", false);
        animator.SetBool("IsAttacking3", false);
        Debug.Log("Combo reset, returning to Idle");
    }
}
