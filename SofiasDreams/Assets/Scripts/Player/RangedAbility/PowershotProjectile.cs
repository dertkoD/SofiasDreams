using System.Collections.Generic;
using UnityEngine;

public class PowershotProjectile : MonoBehaviour
{
    [Header("Tuning")]
    [SerializeField] private float speed = 18f;
    [SerializeField] private int damage = 1;

    [Header("Masks")]
    [SerializeField] private LayerMask damageMask; // enemies (damage, no explode)
    [SerializeField] private LayerMask groundMask; // ground (explode)

    [Header("Colliders (same GameObject)")]
    [SerializeField] private BoxCollider2D damageTrigger;   // isTrigger = true
    [SerializeField] private CapsuleCollider2D groundTrigger; // isTrigger = true

    // Set by spawner
    private Transform _owner;
    private Vector2 _dir = Vector2.right;

    // cached
    private Rigidbody2D _rb;
    private Animator _anim;
    private SpriteRenderer _sr;
    private Vector2 _baseDamageOffset;

    // state
    private bool _exploding;
    private readonly HashSet<Collider2D> _hitOnce = new();

    // ground check
    private ContactFilter2D _groundFilter;
    private readonly Collider2D[] _buf = new Collider2D[4];

    void Awake()
    {
        _rb   = GetComponent<Rigidbody2D>();
        _anim = GetComponent<Animator>();
        _sr   = GetComponentInChildren<SpriteRenderer>() ?? GetComponent<SpriteRenderer>();

        // Rigidbody setup
        _rb.bodyType = RigidbodyType2D.Kinematic;
        _rb.gravityScale = 0f;
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        _rb.freezeRotation = true;

        // Colliders (sizes/offsets are authored in prefab)
        if (!damageTrigger) damageTrigger = GetComponent<BoxCollider2D>();
        if (damageTrigger)
        {
            damageTrigger.isTrigger = true;
            _baseDamageOffset = damageTrigger.offset;
        }

        if (!groundTrigger) groundTrigger = GetComponent<CapsuleCollider2D>();
        if (groundTrigger) groundTrigger.isTrigger = true;

        _groundFilter.useLayerMask = true;
        _groundFilter.layerMask = groundMask;
        _groundFilter.useTriggers = true;
    }

    void OnEnable()
    {
        _exploding = false;
        _hitOnce.Clear();
        if (damageTrigger) damageTrigger.enabled = true;
        if (groundTrigger) groundTrigger.enabled = true;

        ApplyFacing(_dir); // in case Initialize wasn’t called yet
    }

    /// Call right after Instantiate.
    public void Initialize(Transform owner, Vector2 direction)
    {
        _owner = owner;
        ApplyFacing(direction);
    }

    void FixedUpdate()
    {
        if (_exploding) return;

        // Move
        _rb.MovePosition(_rb.position + _dir * speed * Time.fixedDeltaTime);

        // Ground overlap -> explode
        if (groundTrigger && groundTrigger.IsTouchingLayers(groundMask)) StartExplosion();

    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Ignore owner
        if (_owner && (other.transform == _owner || other.transform.IsChildOf(_owner))) return;

        // Damage-only mask
        if (((1 << other.gameObject.layer) & damageMask) != 0)
        {
            if (_hitOnce.Add(other))
            {
                var hb = other.GetComponent<Hurtbox2D>();
                var target = hb ? hb.Owner : null;
                if (target == null || !target.IsAlive)
                    return;

                Vector2 hitPoint = other.ClosestPoint(transform.position);
                target.ApplyDamage(damage, hitPoint, Vector2.zero, gameObject);
            }
        }
    }

    private void ApplyFacing(Vector2 direction)
    {
        _dir = direction.x < 0f ? Vector2.left : Vector2.right;
        int sign = _dir.x < 0f ? -1 : 1;

        // Mirror the whole object (visuals + colliders)
        var s = transform.localScale;
        s.x = Mathf.Abs(s.x) * sign;
        transform.localScale = s;

        if (_sr) _sr.flipX = false; // avoid double flip via flipX

        // Mirror authored damage offset once
        if (damageTrigger)
            damageTrigger.offset = new Vector2(Mathf.Abs(_baseDamageOffset.x) * sign, _baseDamageOffset.y);
    }

    private void StartExplosion()
    {
        _exploding = true;
        if (damageTrigger) damageTrigger.enabled = false;
        if (groundTrigger) groundTrigger.enabled = false;

        speed = 0f;
        if (_anim) _anim.SetTrigger("Explode"); // last frame should call AnimEvent_DestroySelf()
    }

    // Animation Event
    public void AnimEvent_DestroySelf() => Destroy(gameObject);
}