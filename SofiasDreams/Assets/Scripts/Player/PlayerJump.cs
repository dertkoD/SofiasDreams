using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerJump : MonoBehaviour
{
    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float coyoteTime = 0.2f;
    [SerializeField] private float jumpBufferTime = 0.15f;
    
    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.1f;
    
    [Header("Drop-Through")]
    [SerializeField] private float dropDuration = 0.30f; // how long to ignore collisions
    [SerializeField] private KeyCode downKey = KeyCode.S;
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    
    [Header("Refs")]
    [SerializeField] private PlayerAirAttack airAttack;
    [SerializeField] private PlayerAttack attack;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;
    [SerializeField] private Collider2D playerCollider;
    
    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    
    private bool isGrounded;
    private bool isJumping;
    private bool isDropping;

    private readonly List<Collider2D> _contactBuffer = new(8);
    private readonly List<Collider2D> _ignoredThisDrop = new(8);
    
    public void HandleJump()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;

            if (isJumping)
            {
                isJumping = false;
                animator.SetBool("isJumping", false);
            }
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }
        
        // --- DROP-THROUGH comes BEFORE jump input ---
        // Consumes Space on this frame, so the jump buffer below never sees it.
        if (!isDropping
            && Input.GetKey(downKey)
            && Input.GetKeyDown(jumpKey)
            && isGrounded)
        {
            StartCoroutine(DropRoutine());
            return; // <- consume input this frame
        }
        
        if (Input.GetKeyDown(jumpKey) && !attack.IsAttacking)
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        if (jumpBufferCounter > 0f && (coyoteTimeCounter > 0f || isGrounded))
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            isJumping = true;
            isGrounded = false;
            jumpBufferCounter = 0f;
            animator.SetBool("isJumping", true);
        }
    }
    
    private IEnumerator DropRoutine()
    {
        isDropping = true;

        // Stop upward motion so we separate cleanly
        if (rb.linearVelocity.y > 0f)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);

        // Collect current contacts touching the player (no layers needed)
        _contactBuffer.Clear();
        _ignoredThisDrop.Clear();
        
        // Use a permissive filter (no layer or normal constraints)
        var filter = new ContactFilter2D
        {
            useTriggers = false,
            useLayerMask = false,
            useNormalAngle = false,
        };
        
        // IMPORTANT: ensure playerCollider is assigned
        if (playerCollider == null) playerCollider = GetComponent<Collider2D>();

        // Gather overlapping/touching colliders
        playerCollider.attachedRigidbody.GetContacts(filter, _contactBuffer);
        
        // For each contact, if it belongs to a PlatformEffector2D, ignore it
        foreach (var col in _contactBuffer)
        {
            if (col == null) continue;

            // Accept effector on the collider or any parent
            var eff = col.GetComponent<PlatformEffector2D>() ?? col.GetComponentInParent<PlatformEffector2D>();
            if (eff == null) continue;

            Physics2D.IgnoreCollision(playerCollider, col, true);
            _ignoredThisDrop.Add(col);
        }

        // Gentle nudge down helps break contact immediately
        rb.AddForce(Vector2.down * 2.5f, ForceMode2D.Impulse);

        yield return new WaitForSeconds(dropDuration);

        // Re-enable all ignored collisions
        foreach (var col in _ignoredThisDrop)
        {
            if (col == null) continue;
            Physics2D.IgnoreCollision(playerCollider, col, false);
        }
        _ignoredThisDrop.Clear();

        isDropping = false;
    }

    private void FixedUpdate()
    {
        if (airAttack.IsAirAttacking)
        {
            animator.SetFloat("yVelocity", 0f);
        }
        else
        {
            animator.SetFloat("yVelocity", rb.linearVelocity.y);
        }
        
        animator.SetBool("isJumping", !isGrounded);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }

    public bool IsGrounded => isGrounded;
}
