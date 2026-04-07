using UnityEngine;
using UnityEngine.Pool;

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

    IObjectPool<FistProjectile> _pool;

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

    public void SetPool(IObjectPool<FistProjectile> pool)
    {
        _pool = pool;
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
            DissolveAndReturn();
        }
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

        if (_rb) _rb.linearVelocity = Vector2.zero;
        if (_col) _col.enabled = false;

        if (_dissolveController && _dissolveSettings)
        {
            _dissolveController.Play(_dissolveSettings, ReturnToPool);
        }
        else
        {
            ReturnToPool();
        }
    }

    void ReturnToPool()
    {
        if (_pool != null)
        {
            gameObject.SetActive(false);
            _pool.Release(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
