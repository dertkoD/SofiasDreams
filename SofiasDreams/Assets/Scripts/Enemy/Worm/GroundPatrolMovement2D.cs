using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GroundPatrolMovement2D : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 2f;
    public float acceleration = 20f;
    public float deceleration = 25f;
    public WormBrain _brain;
    [SerializeField] public bool hitFlipOnlyInPatrol = true;

    [Header("Ground Check")]
    public LayerMask groundMask;
    public bool useOvalGroundCheck = true;
    public Vector2 groundCheckOvalOffset = new(0f, -0.5f);
    public Vector2 groundCheckOvalSize   = new(0.6f, 0.2f); // горизонтальный овал
    public Vector2 groundCheckOffset     = new(0f, -0.5f);
    public float  groundCheckRadius      = 0.15f;

    [Header("Route (auto)")]
    public EnemyPatrolBinder2D binder;
    public bool abandonRouteOnFirstFail = true;

    [Header("Anti-Stuck")]
    public LayerMask solidLayers;
    public float wallCastDistance = 0.2f;
    public float stuckTimeThreshold = 0.5f;
    public float flipCooldown = 0.35f;

    [Header("Facing / FlipX")]
    public bool flipByScaleX = true;

    [Header("Patrol reaction (damage)")]
    public bool flipOnAttackInPatrol = true;     // наш Hitbox попал в PlayerHurtbox
    public bool flipOnHitInPatrol    = true;     // наш Hurtbox задет PlayerHitbox
    public bool flipOnHpDecreaseInPatrol = true; // пассивно: HP уменьшился
    public LayerMask playerHurtboxLayers;        // trigger
    public LayerMask playerHitboxLayers;         // trigger
    public Collider2D[] hitSources;              // наши Hitbox (trigger)
    public Collider2D[] hurtboxReceivers;        // наши Hurtbox (trigger)

    // runtime
    Rigidbody2D _rb;
    Collider2D  _bodyCol;
    PatrolRoute2D _route;
    int _index;
    float _desiredVX;
    bool _isGrounded;
    float _stuckTimer, _lastPosX;
    int _freeDir = 1;

    int _forceDir;
    float _forceDirTimer;

    // HP watch (без правок чужих скриптов)
    EnemyController _enemy;
    float _lastHp = float.NaN;

    // буферы
    readonly Collider2D[] _colBuf = new Collider2D[16];
    readonly ContactPoint2D[] _cpBuf = new ContactPoint2D[12];
    readonly RaycastHit2D[]   _rhBuf = new RaycastHit2D[12];
    readonly HashSet<Collider2D> _prevPlayerHurtOverlaps = new();
    readonly HashSet<Collider2D> _prevPlayerHitOverlaps  = new();

    const float MIN_WALL_NORM_X = 0.4f;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _bodyCol = GetComponent<Collider2D>();
        if (!binder) binder = GetComponent<EnemyPatrolBinder2D>();
        BindRoute();

        // автопоиск по типам
        if (hitSources == null || hitSources.Length == 0)
            hitSources = GetComponentsInChildren<EnemyContactDamage>(true)
                         .Select(c => c.GetComponent<Collider2D>())
                         .Where(c => c && c.isTrigger).ToArray();

        if (hurtboxReceivers == null || hurtboxReceivers.Length == 0)
            hurtboxReceivers = GetComponentsInChildren<Hurtbox2D>(true)
                               .Select(c => c.GetComponent<Collider2D>())
                               .Where(c => c && c.isTrigger).ToArray();

        // если маски не заданы — попытаться по имени слоя
        if (playerHurtboxLayers.value == 0)
        {
            int l = LayerMask.NameToLayer("PlayerHurtbox");
            if (l >= 0) playerHurtboxLayers = 1 << l;
        }
        if (playerHitboxLayers.value == 0)
        {
            int l = LayerMask.NameToLayer("PlayerHitbox");
            if (l >= 0) playerHitboxLayers = 1 << l;
        }

        // HP observer
        _enemy = GetComponentInParent<EnemyController>();
        if (_enemy != null) _lastHp = GetHp(_enemy);

        var s = transform.localScale;
        if (Mathf.Approximately(s.x, 0f)) { s.x = 1f; transform.localScale = s; }
        _freeDir = s.x >= 0 ? +1 : -1;
        _lastPosX = transform.position.x;
    }

    void BindRoute()
    {
        if (binder) { binder.BindNow(); _route = binder.BoundRoute; }
    }

    void Update()
    {
        _isGrounded = DoGroundCheck();

        _forceDirTimer       = Mathf.Max(0f, _forceDirTimer - Time.deltaTime);

        // пассивный детект «получили урон» по падению HP (только в патруле)
        if (flipOnHpDecreaseInPatrol && _enemy != null && _isGrounded)
        {
            float hp = GetHp(_enemy);
            if (!float.IsNaN(_lastHp) && hp < _lastHp - 0.0001f)
            {
                int face = (_desiredVX != 0f) ? -Mathf.RoundToInt(Mathf.Sign(_desiredVX)) : -_freeDir;
                if (face == 0) face = -1;
                _freeDir = face;
                ForceDirection(face, flipCooldown);
            }
            _lastHp = hp;
        }

        int dir;

        if (_forceDirTimer > 0f)
        {
            dir = _forceDir;
        }
        else if (_route && _route.Count > 0)
        {
            Vector3 target = _route.GetWaypointWorld(_index);
            float dx = target.x - transform.position.x;

            float arrive = Mathf.Max(0.01f, _route.arriveDistance);
            if (Mathf.Abs(dx) <= arrive)
            {
                _index = _route.NextIndexLoop(_index);
                target = _route.GetWaypointWorld(_index);
                dx = target.x - transform.position.x;
            }

            dir = dx > 0f ? +1 : -1;

            if (WallAhead(dir) || IsStuck())
            {
                if (abandonRouteOnFirstFail)
                {
                    _route = null;
                    _freeDir = dir;
                    ForceDirection(_freeDir, flipCooldown);
                    dir = _freeDir;
                }
                else
                {
                    dir = -dir;
                    _index = (_index - 1 + _route.Count) % _route.Count;
                    ForceDirection(dir, flipCooldown);
                }
            }
        }
        else
        {
            dir = (_forceDirTimer > 0f) ? _forceDir : _freeDir;

            if (_forceDirTimer <= 0f && (WallAhead(dir) || IsStuck()))
            {
                _freeDir = -_freeDir;
                dir = _freeDir;
                ForceDirection(dir, flipCooldown);
            }
        }

        if (!_isGrounded) dir = 0;

        _desiredVX = dir * moveSpeed;

        if (flipByScaleX && dir != 0)
        {
            var s = transform.localScale;
            s.x = Mathf.Abs(s.x) * (dir > 0 ? 1 : -1);
            transform.localScale = s;
        }
    }

    void FixedUpdate()
    {
        _isGrounded = DoGroundCheck();

        // реакция на урон в физшаг, чтобы не пропускать однотактные оверлапы
        if (_isGrounded)
        {
            if (flipOnAttackInPatrol && playerHurtboxLayers.value != 0) PatrolReactOnAttack();
            if (flipOnHitInPatrol    && playerHitboxLayers.value  != 0) PatrolReactOnGotHit();
        }

        float vx = _rb.linearVelocity.x;
        float maxDelta = ((Mathf.Abs(_desiredVX) > Mathf.Abs(vx)) ? acceleration : deceleration) * Time.fixedDeltaTime;
        float newVX = Mathf.MoveTowards(vx, _desiredVX, maxDelta);
        _rb.linearVelocity = new Vector2(newVX, _rb.linearVelocity.y);
    }

    // наш Hitbox задел PlayerHurtbox
    void PatrolReactOnAttack()
    {
        if (hitSources == null || hitSources.Length == 0) return;

        var filter = new ContactFilter2D { useLayerMask = true, layerMask = playerHurtboxLayers, useTriggers = true };
        var current = new HashSet<Collider2D>();

        foreach (var src in hitSources)
        {
            if (!src || !src.enabled) continue;
            int n = src.Overlap(filter, _colBuf);
            for (int i = 0; i < n; i++) if (_colBuf[i]) current.Add(_colBuf[i]);
        }

        foreach (var col in current)
        {
            if (_prevPlayerHurtOverlaps.Contains(col)) continue;
            Vector2 p = col.bounds.ClosestPoint(transform.position);
            int face = (p.x >= transform.position.x) ? +1 : -1;
            _freeDir = face;
            ForceDirection(face, flipCooldown);
            break;
        }

        _prevPlayerHurtOverlaps.Clear();
        foreach (var col in current) _prevPlayerHurtOverlaps.Add(col);
    }

    // наш Hurtbox задет PlayerHitbox (мы ПОЛУЧИЛИ урон)
    void PatrolReactOnGotHit()
    {
        if (hurtboxReceivers == null || hurtboxReceivers.Length == 0) return;

        var filter = new ContactFilter2D { useLayerMask = true, layerMask = playerHitboxLayers, useTriggers = true };
        var current = new HashSet<Collider2D>();

        foreach (var hb in hurtboxReceivers)
        {
            if (!hb || !hb.enabled) continue;
            int n = hb.Overlap(filter, _colBuf);
            for (int i = 0; i < n; i++) if (_colBuf[i]) current.Add(_colBuf[i]);
        }

        foreach (var col in current)
        {
            if (_prevPlayerHitOverlaps.Contains(col)) continue; // только вход
            Vector2 p = col.bounds.ClosestPoint(transform.position);
            int face = (p.x >= transform.position.x) ? +1 : -1;
            _freeDir = face;
            ForceDirection(face, flipCooldown);
            break;
        }

        _prevPlayerHitOverlaps.Clear();
        foreach (var col in current) _prevPlayerHitOverlaps.Add(col);
    }

    public void ForceDirection(int dir, float holdSeconds = -1f, bool abandonRoute = false)
    {
        dir = dir >= 0 ? +1 : -1;
        if (abandonRoute) { _route = null; _index = -1; }

        _forceDir = dir;
        _freeDir  = dir;
        _forceDirTimer = (holdSeconds >= 0f ? holdSeconds : flipCooldown);

        if (flipByScaleX)
        {
            var s = transform.localScale;
            s.x = Mathf.Abs(s.x) * dir;
            transform.localScale = s;
        }
    }

    bool DoGroundCheck()
    {
        if (useOvalGroundCheck)
        {
            Vector2 p = (Vector2)transform.position + groundCheckOvalOffset;
            return Physics2D.OverlapCapsule(
                p,
                new Vector2(Mathf.Max(0.001f, groundCheckOvalSize.x), Mathf.Max(0.001f, groundCheckOvalSize.y)),
                CapsuleDirection2D.Horizontal,
                0f,
                groundMask
            );
        }
        else
        {
            Vector2 p = (Vector2)transform.position + groundCheckOffset;
            return Physics2D.OverlapCircle(p, groundCheckRadius, groundMask);
        }
    }

    bool WallAhead(int dir)
    {
        if (!_bodyCol || solidLayers.value == 0 || dir == 0) return false;

        Vector2 castDir = new(Mathf.Sign(dir), 0f);
        var filter = new ContactFilter2D { useLayerMask = true, layerMask = solidLayers, useTriggers = false };

        int c = _rb.GetContacts(filter, _cpBuf);
        for (int i = 0; i < c; i++)
        {
            var n = _cpBuf[i].normal;
            if (Mathf.Abs(n.x) >= MIN_WALL_NORM_X && Mathf.Sign(n.x) == Mathf.Sign(dir)) return true;
        }

        int h = _bodyCol.Cast(castDir, filter, _rhBuf, Mathf.Max(0.01f, wallCastDistance));
        for (int i = 0; i < h; i++)
        {
            var n = _rhBuf[i].normal;
            if (Mathf.Abs(n.x) >= MIN_WALL_NORM_X && Mathf.Sign(n.x) == Mathf.Sign(dir)) return true;
        }
        return false;
    }

    bool IsStuck()
    {
        float moved = Mathf.Abs(transform.position.x - _lastPosX);
        _lastPosX = transform.position.x;

        if (moved < 0.002f && Mathf.Abs(_desiredVX) > 0.1f)
        {
            _stuckTimer += Time.deltaTime;
            if (_stuckTimer >= stuckTimeThreshold) { _stuckTimer = 0f; return true; }
        }
        else _stuckTimer = 0f;

        return false;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        if (useOvalGroundCheck)
        {
            Vector3 p = transform.position + (Vector3)groundCheckOvalOffset;
            Matrix4x4 prev = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(
                p, Quaternion.identity,
                new Vector3(Mathf.Max(0.001f, groundCheckOvalSize.x), Mathf.Max(0.001f, groundCheckOvalSize.y), 1f)
            );
            Gizmos.DrawWireSphere(Vector3.zero, 0.5f);
            Gizmos.matrix = prev;
        }
        else
        {
            Vector3 p = transform.position + (Vector3)groundCheckOffset;
            Gizmos.DrawWireSphere(p, groundCheckRadius);
        }

        Gizmos.color = Color.cyan;
        float sign = Mathf.Sign((_desiredVX == 0f) ? (_freeDir == 0 ? 1f : _freeDir) : _desiredVX);
        Vector3 from = transform.position + Vector3.up * 0.05f;
        Gizmos.DrawLine(from, from + Vector3.right * sign * wallCastDistance);
    }
#endif

    // — HP reflection helpers —
    static float GetHp(EnemyController ec)
    {
        var t = ec.GetType();

        var p = t.GetProperty("CurrentHealth") ?? t.GetProperty("Health") ?? t.GetProperty("HP");
        if (p != null) return ConvertToFloat(p.GetValue(ec, null));

        var f = t.GetField("currentHealth") ?? t.GetField("health") ?? t.GetField("hp");
        if (f != null) return ConvertToFloat(f.GetValue(ec));

        return float.NaN;
    }
    static float ConvertToFloat(object v)
    {
        if (v == null) return float.NaN;
        if (v is float f) return f;
        if (v is int i) return i;
        if (v is double d) return (float)d;
        return float.TryParse(v.ToString(), out var r) ? r : float.NaN;
    }
    
    public void FlipOnDamageContact(bool abandonRoute = true, float holdSeconds = -1f)
    {
        if (hitFlipOnlyInPatrol)
        {
            _brain ??= GetComponentInParent<WormBrain>();
            if (_brain && !_brain.IsInPatrol) return; // агро/атака/стан — выходим
        }

        int curDir = Mathf.Abs(_desiredVX) >= 0.01f
            ? (_desiredVX > 0f ? +1 : -1)
            : (transform.localScale.x >= 0f ? +1 : -1);

        if (_rb) _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
        ForceDirection(-curDir, (holdSeconds >= 0f ? holdSeconds : flipCooldown), abandonRoute);
    }
}
