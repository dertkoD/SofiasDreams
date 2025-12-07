using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerAttack playerAttack;

    [Header("Grounding")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.12f;
    [SerializeField] private LayerMask groundMask;

    [Header("Input")]
    [SerializeField] private float inputDeadzone = 0.05f;

    // --- Grapple momentum (hard lock + soft carry) ---
    private Vector2 _extVel = Vector2.zero;
    private bool _overrideX = true;
    private bool _overrideY = false;
    private float _hardLockUntil = -1f;  // during this, force ext velocity each frame
    private float _softCarryUntil = -1f; // after hard lock, preserve X until steer/ground/timeout

    private float horizontalInput;
    private bool isFacingRight = true;
    private bool movementLocked = false;

    // NEW: hard lock + soft carry API
    public void SetExternalVelocity(Vector2 v, float hardLockDuration, float softCarryMaxDuration,
                                    bool overrideX = true, bool overrideY = false)
    {
        _extVel = v;
        _overrideX = overrideX;
        _overrideY = overrideY;
        float now = Time.time;
        _hardLockUntil   = now + Mathf.Max(0f, hardLockDuration);
        _softCarryUntil  = now + Mathf.Max(hardLockDuration, softCarryMaxDuration); // soft >= hard
    }

    // Back-compat (if you still call the old signature)
    public void SetExternalVelocity(Vector2 v, float duration, bool overrideX = true, bool overrideY = false)
        => SetExternalVelocity(v, duration, duration, overrideX, overrideY);

    public void HandleMoveInput()
    {
        float now = Time.time;

        // 1) HARD LOCK: fully enforce external velocity (no one can stomp it)
        if (now < _hardLockUntil)
        {
            var v = rb.linearVelocity;
            if (_overrideX) v.x = _extVel.x;
            if (_overrideY) v.y = _extVel.y;
            rb.linearVelocity = v;

            animator.SetFloat("xVelocity", Mathf.Abs(rb.linearVelocity.x));
            FlipSpriteFromVelocity(rb.linearVelocity.x);
            return;
        }

        // 2) LOCKS/ATTACKS: snap X to 0 (snappy ground-like feel)
        if (movementLocked || (playerAttack != null && playerAttack.IsAttacking))
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            animator.SetFloat("xVelocity", 0f);
            return;
        }

        // 3) Read input
        horizontalInput = Input.GetAxisRaw("Horizontal");
        bool hasInput = Mathf.Abs(horizontalInput) > inputDeadzone;

        // 4) SOFT CARRY: if within soft window AND no input AND not grounded -> preserve X (do nothing to X)
        // Ends as soon as player steers or becomes grounded or timeout hits.
        bool inSoftCarryWindow = now < _softCarryUntil;
        if (inSoftCarryWindow && !hasInput && !IsGrounded())
        {
            // do not touch rb.linearVelocity.x here (keep grapple momentum)
            animator.SetFloat("xVelocity", Mathf.Abs(rb.linearVelocity.x));
            FlipSpriteFromVelocity(rb.linearVelocity.x);
            return;
        }

        // 5) Normal movement (no inertia unless from grapple windows)
        if (hasInput)
        {
            rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);
        }
        else
        {
            // No input: snap to 0 (grounded AND in-air) — prevents ledge-walk inertia.
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }

        animator.SetFloat("xVelocity", Mathf.Abs(rb.linearVelocity.x));
        FlipSprite();
    }

    private bool IsGrounded()
    {
        if (groundCheck == null) return false;
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundMask);
    }

    private void FlipSprite()
    {
        if ((isFacingRight && horizontalInput < 0f) || (!isFacingRight && horizontalInput > 0f))
        {
            isFacingRight = !isFacingRight;
            var s = transform.localScale; s.x *= -1f; transform.localScale = s;
        }
    }

    private void FlipSpriteFromVelocity(float xVel)
    {
        if ((isFacingRight && xVel < -0.01f) || (!isFacingRight && xVel > 0.01f))
        {
            isFacingRight = !isFacingRight;
            var s = transform.localScale; s.x *= -1f; transform.localScale = s;
        }
    }

    public void SetMovementLocked(bool locked)
    {
        if (movementLocked == locked) return;
        movementLocked = locked;

        if (locked)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            animator.SetFloat("xVelocity", 0f);
        }
    }

    public bool IsMovementLocked => movementLocked;

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = new Color(0f, 1f, 0.4f, 0.35f);
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
