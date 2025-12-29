using UnityEngine;

public class FistProjectile : MonoBehaviour
{
    [SerializeField] float speed = 10f;
    [SerializeField] float lifetime = 5f;
    [SerializeField] int damage = 1;
    [SerializeField] LayerMask groundLayers;

    Rigidbody2D _rb;
    float _dieAt;
    
    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    void OnEnable()
    {
        _dieAt = Time.time + lifetime;
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
        if (Time.time >= _dieAt)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Check for player hurtbox or damageable
        IDamageable target = other.GetComponent<IDamageable>();
        if (target == null)
        {
             var hurtbox = other.GetComponent<Hurtbox2D>();
             if (hurtbox != null) target = hurtbox.GetComponentInParent<IDamageable>();
        }

        if (target != null)
        {
            target.ApplyDamage(damage, transform.position, Vector2.zero, gameObject);
            Destroy(gameObject);
        }
        else if (other.GetComponent<Hurtbox2D>() != null)
        {
            Destroy(gameObject);
        }
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (((1 << col.gameObject.layer) & groundLayers.value) != 0)
        {
            Destroy(gameObject);
        }
    }
}
