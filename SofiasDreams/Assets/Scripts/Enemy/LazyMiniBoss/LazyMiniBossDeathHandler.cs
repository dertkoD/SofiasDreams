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

    [SerializeField] DissolveVfxSettingsSO _dissolveSettings;
    [SerializeField] SpriteDissolveController _dissolveController;
    [SerializeField] LayerMask _groundLayer; // For detecting landing

    bool _dead;

    void Awake()
    {
        if (!_health) _health = GetComponent<Health>();
        if (!_rb) _rb = GetComponent<Rigidbody2D>();
        if (!_anim) _anim = GetComponent<LazyMiniBossAnimatorAdapter>();
        if (!_mainCollider) _mainCollider = GetComponent<Collider2D>();
        if (!_dissolveController) _dissolveController = GetComponent<SpriteDissolveController>();
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
        // 1. Wait until falling (velocity negative) or platform broken event logic handles physics enable.
        // We wait for some downward velocity or a timeout to confirm we are falling.
        float waitStart = Time.time;
        while (Time.time < waitStart + 5f && (_rb.linearVelocity.y >= -0.1f))
        {
            yield return null;
        }

        // 2. Wait until landed on ground (below platform)
        // We check if we hit something in ground layer.
        bool landed = false;
        while (!landed)
        {
            // Simple check: is velocity near zero? Or Raycast down?
            // Since we fall from high, impact might be high velocity -> zero.
            
            // Check if grounded using Raycast or Collider
            if (_rb.linearVelocity.y > -0.1f && _rb.linearVelocity.y < 0.1f)
            {
                 // Possibly landed or stuck. Check ground.
                 if (Physics2D.Raycast(transform.position, Vector2.down, 1.5f, _groundLayer))
                 {
                     landed = true;
                 }
            }
            
            // Safety: if we fell too far (off world), destroy anyway.
            if (transform.position.y < -100f)
            {
                Destroy(gameObject);
                yield break;
            }

            yield return new WaitForSeconds(0.1f);
        }

        // 3. Play Dissolve
        if (_dissolveController && _dissolveSettings)
        {
            bool dissolveFinished = false;
            _dissolveController.Play(_dissolveSettings, () => dissolveFinished = true);
            
            while (!dissolveFinished) yield return null;
        }
        else
        {
            yield return new WaitForSeconds(1f);
        }

        // 4. Destroy
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
