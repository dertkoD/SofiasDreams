using UnityEngine;

/// <summary>
/// Projectile used by BullBullet (LazyMiniBoss Attack3).
/// Flies along a fixed ballistic arc from its spawn position to a target position
/// that is computed at the moment of firing (last seen player position by default).
/// It does NOT home onto the player: once fired, the trajectory is fully determined.
/// </summary>
public class BullArcProjectile : MonoBehaviour
{
    [Header("Combat")]
    [SerializeField] int damage = 1;
    [SerializeField] float lifetime = 5f;
    [SerializeField] LayerMask groundLayers;

    [Header("Arc")]
    [Tooltip("Flight time from muzzle to target, in seconds.")]
    [Min(0.05f)] [SerializeField] float travelTime = 1.0f;
    [Tooltip("Extra peak height of the parabola above the straight line between muzzle and target.")]
    [Min(0f)] [SerializeField] float arcHeight = 3.0f;
    [Tooltip("If true, rotates the projectile so that its local right axis follows the velocity vector.")]
    [SerializeField] bool rotateTowardsVelocity = true;

    [Header("VFX")]
    [SerializeField] DissolveVfxSettingsSO _dissolveSettings;

    Rigidbody2D _rb;
    SpriteDissolveController _dissolveController;
    Collider2D _col;

    Vector2 _velocity;
    float _gravity;
    float _dieAt;
    bool _isDissolving;
    bool _hasHitPlayer;
    bool _inFlight;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _dissolveController = GetComponent<SpriteDissolveController>();
        _col = GetComponent<Collider2D>();
    }

    void OnEnable()
    {
        _dieAt = Time.time + lifetime;
        _isDissolving = false;
        _hasHitPlayer = false;
        _inFlight = false;
        _velocity = Vector2.zero;
        _gravity = 0f;
        if (_col) _col.enabled = true;
        if (_rb) _rb.simulated = true;
    }

    public void Setup(int dmg)
    {
        damage = dmg;
    }

    /// <summary>
    /// Fires the projectile so that it follows a parabola from its current position
    /// to <paramref name="targetPosition"/>. The arc is determined by the prefab
    /// settings (travelTime and arcHeight). Optional overrides allow the shooter
    /// to tune the arc per shot.
    /// </summary>
    public void Fire(Vector2 targetPosition, float travelTimeOverride = -1f, float arcHeightOverride = -1f)
    {
        float T = travelTimeOverride > 0f ? travelTimeOverride : travelTime;
        float h = arcHeightOverride >= 0f ? arcHeightOverride : arcHeight;
        if (T < 0.05f) T = 0.05f;

        Vector2 origin = transform.position;
        float dx = targetPosition.x - origin.x;
        float dy = targetPosition.y - origin.y;

        // Derived from projectile motion with fixed travel time T and extra peak
        // height h above the midpoint of the O->T segment:
        //   g  = 8 * h / T^2
        //   vy = (dy + 4 * h) / T
        //   vx = dx / T
        _gravity = (8f * h) / (T * T);
        float vx = dx / T;
        float vy = (dy + 4f * h) / T;
        _velocity = new Vector2(vx, vy);
        _inFlight = true;

        if (_rb)
        {
            _rb.linearVelocity = _velocity;
        }

        ApplyRotation();
    }

    void FixedUpdate()
    {
        if (_isDissolving || !_inFlight) return;

        _velocity.y -= _gravity * Time.fixedDeltaTime;

        if (_rb)
        {
            _rb.linearVelocity = _velocity;
        }
        else
        {
            transform.position += (Vector3)(_velocity * Time.fixedDeltaTime);
        }
    }

    void Update()
    {
        if (_isDissolving) return;

        if (_inFlight && rotateTowardsVelocity)
            ApplyRotation();

        if (Time.time >= _dieAt)
        {
            DissolveAndReturn();
        }
    }

    void ApplyRotation()
    {
        if (!rotateTowardsVelocity) return;
        if (_velocity.sqrMagnitude < 0.0001f) return;
        transform.right = (Vector3)_velocity.normalized;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_isDissolving) return;

        IDamageable target = other.GetComponent<IDamageable>();
        if (target == null)
        {
            var hurtbox = other.GetComponent<Hurtbox2D>();
            if (hurtbox != null) target = hurtbox.GetComponentInParent<IDamageable>();
        }

        if (target != null)
        {
            if (!_hasHitPlayer)
            {
                target.ApplyDamage(damage, transform.position, Vector2.zero, gameObject);
                _hasHitPlayer = true;
            }
        }
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (_isDissolving) return;

        if (((1 << col.gameObject.layer) & groundLayers.value) != 0)
        {
            DissolveAndReturn();
        }
    }

    void DissolveAndReturn()
    {
        if (_isDissolving) return;
        _isDissolving = true;
        _inFlight = false;

        if (_rb)
        {
            _rb.linearVelocity = Vector2.zero;
            _rb.simulated = false;
        }

        if (_col) _col.enabled = false;

        if (_dissolveController && _dissolveSettings)
        {
            _dissolveController.Play(_dissolveSettings, ReturnToPoolOrDestroy);
        }
        else
        {
            ReturnToPoolOrDestroy();
        }
    }

    void ReturnToPoolOrDestroy()
    {
        var pe = GetComponent<PooledEntity>();
        if (pe != null)
            pe.ReturnToPool();
        else
            Destroy(gameObject);
    }
}
