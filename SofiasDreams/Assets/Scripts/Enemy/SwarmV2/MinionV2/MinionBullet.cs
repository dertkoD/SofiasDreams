using UnityEngine;

public class MinionBullet : MonoBehaviour
{
    [Header("Motion")]
    [SerializeField] float speed = 10f;
    [SerializeField] float lifetime = 3f;

    [Header("Collision")]
    [SerializeField] LayerMask groundLayers;   // слой(я) стен/пола (НЕ триггер)

    [Header("Word (показывать после Appearance)")]
    [SerializeField] GameObject wordRoot;
    [SerializeField] SpriteRenderer wordRenderer;
    [SerializeField] Sprite[] wordSprites;
    [SerializeField] bool roundRobin = true;

    [Header("Word stabilizer")]
    [SerializeField] bool keepWordUpright = true;
    [SerializeField] bool preventWordMirror = true;

    Rigidbody2D _rb;
    Animator _anim;
    Collider2D[] _allCols;
    Transform _wordT;
    float _dieAt;
    int _appearanceHash;

    static int s_nextWordIndex = 0;
    int _lastRandomIndex = -1;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        if (_rb)
        {
            _rb.gravityScale = 0f;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rb.freezeRotation = true;
        }

        _allCols = GetComponentsInChildren<Collider2D>(true);

        // Корневой коллайдер можно оставить триггером (Hitbox). "Body" — НЕ триггер, на дочернем объекте.
        var rootCol = GetComponent<Collider2D>();
        if (rootCol) rootCol.isTrigger = true;

        _anim = GetComponentInChildren<Animator>(true);
        _appearanceHash = Animator.StringToHash("Appearance");
        if (_anim) _anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        if (!wordRoot)
        {
            var t = transform.Find("Word");
            if (t) wordRoot = t.gameObject;
        }
        if (!wordRenderer && wordRoot)
            wordRenderer = wordRoot.GetComponentInChildren<SpriteRenderer>(true);

        _wordT = wordRoot ? wordRoot.transform : null;
    }

    void OnEnable()
    {
        if (_rb)
        {
            _rb.simulated = true;
            _rb.linearVelocity = Vector2.zero;
            _rb.angularVelocity = 0f;
        }

        _dieAt = Time.time + lifetime;

        if (wordRoot) wordRoot.SetActive(false);
        transform.localScale = Vector3.one;
        if (_wordT) _wordT.localScale = Vector3.one;

        AssignNextWordSprite();

        if (_anim)
        {
            _anim.enabled = true;
            _anim.speed = 1f;
            _anim.Rebind(); _anim.Update(0f);
            _anim.Play(_appearanceHash, 0, 0f);
            _anim.Update(0f);
        }

        if (_allCols != null)
            for (int i = 0; i < _allCols.Length; i++)
                if (_allCols[i]) _allCols[i].enabled = true;
    }

    public void Fire(Vector2 direction)
    {
        var dir = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
        transform.right = dir;
        if (_rb) _rb.linearVelocity = dir * speed;
        _dieAt = Time.time + lifetime;
    }

    void Update()
    {
        if (Time.time >= _dieAt)
        {
            ReturnToPoolOrDestroy();
            return;
        }

        if (_wordT)
        {
            if (keepWordUpright)
            {
                var e = _wordT.eulerAngles;
                e.z = 0f;
                _wordT.eulerAngles = e;
            }
            if (preventWordMirror)
            {
                var ls = _wordT.localScale;
                if (ls.x < 0f) ls.x = -ls.x;
                _wordT.localScale = ls;
            }
        }
    }

    // Триггер-хитбокс: игрок/триггерные поверхности
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<Hurtbox2D>() != null)
        {
            ReturnToPoolOrDestroy();
        }
    }

    // НЕ триггерный Body-коллайдер ловит жёсткие коллизии со стенами/полом
    void OnCollisionEnter2D(Collision2D col)
    {
        if (((1 << col.gameObject.layer) & groundLayers.value) != 0)
        {
            ReturnToPoolOrDestroy();
        }
    }

    void ReturnToPoolOrDestroy()
    {
        if (_allCols != null)
            for (int i = 0; i < _allCols.Length; i++)
                if (_allCols[i]) _allCols[i].enabled = false;

        var pe = GetComponent<PooledEntity>();
        if (pe != null) pe.ReturnToPool();
        else Destroy(gameObject);
    }

    // Animation Event: конец Appearance
    public void AnimationEvent_AppearanceEnd_ShowWord()
    {
        if (wordRoot) wordRoot.SetActive(true);
    }

    void AssignNextWordSprite()
    {
        if (!wordRenderer || wordSprites == null || wordSprites.Length == 0) return;

        int idx;
        if (roundRobin)
        {
            idx = s_nextWordIndex % wordSprites.Length;
            s_nextWordIndex = (s_nextWordIndex + 1) % int.MaxValue;
        }
        else
        {
            if (wordSprites.Length == 1) idx = 0;
            else
            {
                do { idx = Random.Range(0, wordSprites.Length); }
                while (idx == _lastRandomIndex);
                _lastRandomIndex = idx;
            }
        }
        wordRenderer.sprite = wordSprites[idx];
    }
}
