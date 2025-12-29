using UnityEngine;

public class FistProjectile : MonoBehaviour
{
    [SerializeField] float speed = 10f;
    [SerializeField] float lifetime = 5f;
    [SerializeField] int damage = 1;
    [SerializeField] LayerMask groundLayers;
    
    [Header("VFX")]
    [SerializeField] DissolveVfxSettingsSO _dissolveSettings;

    Rigidbody2D _rb;
    SpriteDissolveController _dissolveController;
    Collider2D _col;
    float _dieAt;
    bool _isDissolving;
    bool _hasHitPlayer;
    
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
        if (_col) _col.enabled = true;
    }

    public void Setup(int dmg)
    {
        damage = dmg;
    }

    public void Fire(Vector2 direction, float speedOverride = -1f)
    {
        float s = speedOverride > 0 ? speedOverride : speed;
        if (_rb)
        {
            _rb.linearVelocity = direction.normalized * s;
            transform.right = direction;
        }
    }

    void Update()
    {
        if (_isDissolving) return;

        if (Time.time >= _dieAt)
        {
            DissolveAndDestroy();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_isDissolving) return;

        // Check for player hurtbox or damageable
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
            DissolveAndDestroy();
        }
    }

    void DissolveAndDestroy()
    {
        if (_isDissolving) return;
        _isDissolving = true;

        // Stop movement
        if (_rb) _rb.linearVelocity = Vector2.zero;
        
        // Disable collider to prevent further hits
        if (_col) _col.enabled = false;

        if (_dissolveController && _dissolveSettings)
        {
            _dissolveController.Play(_dissolveSettings, () => Destroy(gameObject));
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
