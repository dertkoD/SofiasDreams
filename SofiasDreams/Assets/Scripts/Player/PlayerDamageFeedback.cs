using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerDamageFeedback : MonoBehaviour
{
    [Header("Refs (optional but recommended)")]
    [SerializeField] private Animator animator; // optional "Hurt" trigger
    [SerializeField] private PlayerController playerController; // your movement script; will be temporarily disabled
    [SerializeField] private PlayerHealth playerHealth; // any script that has a public TakeDamage(int) method
    
    [Header("Knockback")]
    [Tooltip("Horizontal impulse applied away from the damage source.")]
    [SerializeField] private float knockbackX = 6.5f;
    [Tooltip("Vertical impulse applied on hit (small pop).")]
    [SerializeField] private float knockbackY = 3.0f;
    [Tooltip("If true, reset horizontal velocity before applying knockback so it feels consistent.")]
    [SerializeField] private bool zeroHorizontalBeforeKnockback = true;
    
    [Header("Hit Stun")]
    [Tooltip("Player control is paused for this long (seconds) after a hit.")]
    [SerializeField] private float hitStunDuration = 0.18f;
    
    private Rigidbody2D _rb;
    private int _hurtTriggerId;
    
    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        if (!playerHealth) playerHealth = GetComponent<PlayerHealth>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        _hurtTriggerId = Animator.StringToHash("Hurt");
    }

    public void ApplyDamageWithKnockback(int damage, Vector2 sourcePosition)
    {
        // Respect bonfire protection if you use it.
        var bonfire = FindObjectOfType<BonfireManager>();
        if (bonfire && bonfire.IsAtBonfire) return;

        if (!playerHealth) return; // safety

        // If already invincible, skip entirely (no knockback spam during i-frames)
        // Requires a public bool IsInvincible on PlayerHealth (one-liner accessor)
        if (playerHealth.IsInvincible) return;

        int hpBefore = playerHealth.GetCurrentHP();
        playerHealth.TakeDamage(damage);
        bool damageApplied = playerHealth.GetCurrentHP() < hpBefore;
        if (!damageApplied) return; //sanity guard in case health logic vetoed damage

        var atk = GetComponent<PlayerAttack>();
        if (atk) atk.InterruptAttack();
        
        // Optional animation
        if (animator) animator.SetTrigger(_hurtTriggerId);
        
        // Knockback direction: away from source.
        float dir = Mathf.Sign(transform.position.x - sourcePosition.x);
        if (dir == 0) dir = -Mathf.Sign(_rb.linearVelocity.x == 0 ? 1f : _rb.linearVelocity.x);
        
        if (zeroHorizontalBeforeKnockback)
        {
            _rb.linearVelocity = new Vector2(0f, Mathf.Min(_rb.linearVelocity.y, 0f));
        }

        _rb.linearVelocity = new Vector2(knockbackX * dir, knockbackY);

        // Brief hit-stun (control lock) only when damage was applied.
        if (playerController) StartCoroutine(HitStunCR());
    }
    
    private IEnumerator HitStunCR()
    {
        if (playerController) playerController.enabled = false;
        yield return new WaitForSeconds(hitStunDuration);
        if (playerController) playerController.enabled = true;
    }
}
