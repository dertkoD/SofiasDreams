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
            // Only start sequence if not triggered by event
            StartCoroutine(DeathSequenceMonitor());
        }
    }

    IEnumerator DeathSequenceMonitor()
    {
        // Monitor fall and destroy
        
        // Wait until fallen
        while (transform.position.y > _fallCheckYThreshold)
        {
            yield return new WaitForSeconds(0.5f);
        }

        // Destroy
        Destroy(gameObject);
    }

    // Called by Animation Event
    public void AnimationEvent_BreakPlatform()
    {
        // 1. Break Platform
        if (_platformBreaker)
        {
            _platformBreaker.BreakAll();
        }
        else
        {
            var breaker = FindObjectOfType<BossPlatformBreaker>();
            if (breaker) breaker.BreakAll();
        }

        // 2. Ensure physics allows falling
        if (_rb)
        {
             _rb.simulated = true;
             _rb.linearVelocity = Vector2.zero;
             _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
             _rb.gravityScale = 3f;
        }
    }
}
