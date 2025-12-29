using System.Collections;
using UnityEngine;
using Zenject;

public class LazyMiniBossDeathHandler : MonoBehaviour
{
    [SerializeField] Health _health;
    [SerializeField] Rigidbody2D _rb;
    [SerializeField] Collider2D _mainCollider;
    [SerializeField] LazyMiniBossAnimatorAdapter _anim;
    
    // Assign the platform breaker in inspector or find it
    [SerializeField] BossPlatformBreaker _platformBreaker; 

    [SerializeField] float _fallDestroyDelay = 3f;
    [SerializeField] float _fallCheckYThreshold = -50f; // Y-coord below which we consider fallen

    bool _dead;

    void Awake()
    {
        if (!_health) _health = GetComponent<Health>();
        if (!_rb) _rb = GetComponent<Rigidbody2D>();
        if (!_anim) _anim = GetComponent<LazyMiniBossAnimatorAdapter>();
        if (!_mainCollider) _mainCollider = GetComponent<Collider2D>();
    }

    void OnEnable()
    {
        if (_health) _health.OnHealthChanged += OnHealthChanged;
    }

    void OnDisable()
    {
        if (_health) _health.OnHealthChanged -= OnHealthChanged;
    }

    void OnHealthChanged()
    {
        if (_dead) return;
        if (!_health.IsAlive)
        {
            _dead = true;
            StartCoroutine(DeathSequence());
        }
    }

    IEnumerator DeathSequence()
    {
        // 1. Ensure animation trigger (Brain handles this too, but let's be sure)
        // Actually Brain enters "Death" state which triggers anim.
        // Wait until animation finishes. 
        // We can check Animator state or just wait a fixed time if we know clip length.
        // Better: Wait until we are in Death state, then wait until normalized time >= 1.
        
        // Wait for next frame to allow Animator to update state
        yield return null; 
        
        // Wait until we are definitely in Death animation (or timeout)
        float timeout = 2f;
        while (timeout > 0 && !_anim.IsInState("Death"))
        {
             timeout -= Time.deltaTime;
             yield return null;
        }

        // Wait for animation to finish
        // Note: AnimatorAdapter doesn't expose normalized time yet.
        // Let's assume a fixed wait or use simple check
        Animator anim = GetComponentInChildren<Animator>();
        if (anim)
        {
             yield return new WaitForSeconds(anim.GetCurrentAnimatorStateInfo(0).length);
        }
        else
        {
             yield return new WaitForSeconds(1f);
        }

        // 2. Break Platform
        if (_platformBreaker)
        {
            _platformBreaker.BreakAll();
        }
        else
        {
            // Fallback: search scene?
            var breaker = FindObjectOfType<BossPlatformBreaker>();
            if (breaker) breaker.BreakAll();
        }

        // 3. Ensure physics allows falling
        // If "Death" animation disabled physics or froze Constraints, we must enable falling.
        if (_rb)
        {
             _rb.simulated = true;
             _rb.linearVelocity = Vector2.zero; // Reset any movement
             _rb.constraints = RigidbodyConstraints2D.FreezeRotation; // Allow Y movement
             _rb.gravityScale = 3f; // Fall fast
        }
        
        // Also disable collider interacting with Player but keep interacting with environment?
        // If we want him to fall through "ground" that isn't the platform, we might need to change layers.
        // But if the platform disappears, he should naturally fall.
        // However, if he is on a "OneWayPlatform" or "Ground", we rely on the platform disappearing.

        // Wait until fallen
        while (transform.position.y > _fallCheckYThreshold)
        {
            yield return new WaitForSeconds(0.5f);
        }

        // Destroy
        Destroy(gameObject);
    }
}
