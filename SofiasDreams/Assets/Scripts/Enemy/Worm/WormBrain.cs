using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WormBrain : MonoBehaviour, IExternalHitStunHost
{
   public enum State { Patrol, Windup, Spin, StunArc, StunHold }
    enum BounceCause { None, Player, Wall }

    [Header("Refs")]
    public GroundPatrolMovement2D patrol;
    public VisionCone2D vision;          // узкий FOV для входа в агро
    public Animator animator;
    public EnemyStateMirror mirror;
    public Rigidbody2D rb;
    public Collider2D bodyCollider;
    public Transform facingTransform;
    public LedgeGuard2D ledgeGuard;
    public bool useLedgeGuard = true;
    
    [Header("Aggro")]
    public float aggroTimeout = 2.0f; // настраивается в инспекторе
    float _aggroLeft = 0f;

    [Header("Layers")]
    public LayerMask playerHurtboxLayers;
    public LayerMask solidLayers;

    [Header("Windup")]  public float windupTime = 0.12f; public float windupDamp = 20f;
    [Header("Charge")]  public float chargeSpeed = 10f;  public float chargeAcceleration = 100f; public float spinMinDuration = 0.10f;
    [Header("Bounce")]  public float bounceArcDistance = 1.5f; public float bounceArcHeight = 0.8f;
    [Header("Stun")]    public float stunStandDuration = 0.6f; public float stunHoldLinearDrag = 20f;
    [Header("Visual")]  [SerializeField] bool flipByScaleX = true;

    // [NEW] наши Hitbox-триггеры для детекта попадания по игроку во время Spin
    [Header("Hit detect (Spin only)")]
    public Collider2D[] hitSources;

    static readonly int hTriggerAttack = Animator.StringToHash("TriggerAttack");
    static readonly int hSpinningTrig  = Animator.StringToHash("SpinningTrigger");
    static readonly int hStunTrigger   = Animator.StringToHash("StunTrigger");
    static readonly int hPatrolTrigger = Animator.StringToHash("PatrolTrigger");

    State _state;
    Vector2 _chargeDir;
    float _stateT, _savedDrag, _stunTimeLeft;
    int _facingSign = +1;
    Vector2 _targetSnapshotPos; bool _hasSnapshot;
    int  _lockedFaceSign = +1; bool _lockFaceTillStunEnd = false;

    [HideInInspector] public bool externalHitStunActive;
    int _queuedSpinDir = 0; int _lastMoveSign = +1;

    readonly Collider2D[] _colBuf = new Collider2D[16];
    readonly ContactPoint2D[] _cpBuf = new ContactPoint2D[12];
    readonly RaycastHit2D[] _rhBuf = new RaycastHit2D[12];

    const float MIN_HORIZ=0.4f, MIN_FACING=0.35f, MIN_GROUND=0.7f;
    public bool ExternalHitStunActive { get=>externalHitStunActive; set=>externalHitStunActive=value; }
    public bool IsInPatrol => _state == State.Patrol;

    void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (!bodyCollider) bodyCollider = GetComponent<Collider2D>();
        if (!animator) animator = GetComponentInChildren<Animator>(true);
        if (!mirror) mirror = GetComponentInParent<EnemyStateMirror>();
        if (!patrol) patrol = GetComponent<GroundPatrolMovement2D>();
        if (!vision) vision = GetComponentInChildren<VisionCone2D>(true);
        if (!facingTransform) facingTransform = transform;
        if (!ledgeGuard) ledgeGuard = GetComponentInChildren<LedgeGuard2D>(true);

        // [NEW] автосбор Hitbox-коллайдеров, если не назначены
        if (hitSources == null || hitSources.Length == 0)
        {
            hitSources = GetComponentsInChildren<EnemyContactDamage>(true)
                         .Select(c => c.GetComponent<Collider2D>())
                         .Where(c => c && c.isTrigger && c.enabled)
                         .ToArray();
        }

        _savedDrag = rb.linearDamping;
        _facingSign = SignFromScale(facingTransform);
        SwitchTo(State.Patrol, true);
    }

    void OnEnable()=>SwitchTo(State.Patrol,true);

    void Update()
    {
        if (externalHitStunActive) return;
        _stateT += Time.deltaTime;

        if (_state is State.Patrol or State.Windup)
            _facingSign = SignFromScale(facingTransform);

        if (useLedgeGuard && ledgeGuard)
            ledgeGuard.SetFacingSign(_lockFaceTillStunEnd ? _lockedFaceSign : _facingSign);

        switch (_state)
        {
            case State.Patrol:
                if (vision && vision.TryGetClosestTarget(out var t)) BeginWindup(t);
                break;

            case State.Windup:
                rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, Vector2.zero, windupDamp * Time.deltaTime);
                if (_stateT >= windupTime) BeginChargeFromSnapshot();
                break;

            case State.StunArc:
                if (IsGrounded())
                {
                    rb.linearVelocity = Vector2.zero;
                    rb.angularVelocity = 0f;
                    rb.linearDamping = stunHoldLinearDrag;
                    Fire(hStunTrigger);
                    SwitchTo(State.StunHold);
                }
                break;

            case State.StunHold:
                _stunTimeLeft -= Time.deltaTime;
                if (_stunTimeLeft <= 0f)
                {
                    // заменяем visionSense: решаем по таймеру агро
                    if (_aggroLeft > 0f)
                    {
                        // можно навестись на текущую цель из vision, если она есть
                        int dir = (vision && vision.TryGetClosestTarget(out var tSense)) ? DirTo(tSense) : _lastMoveSign;
                        BeginWindupForcedTrigger(dir);
                    }
                    else SwitchTo(State.Patrol);
                }
                break;
        }

        // [НОВОЕ] тик и продление агро-таймера во всех НЕ Patrol состояниях
        if (_state != State.Patrol)
        {
            if (vision && vision.TryGetClosestTarget(out _)) _aggroLeft = aggroTimeout; // продлеваем
            else _aggroLeft = Mathf.Max(0f, _aggroLeft - Time.deltaTime);               // тикаем
        }
    }

    void FixedUpdate()
    {
        if (externalHitStunActive || _state != State.Spin) return;

        if (Mathf.Abs(rb.linearVelocity.x) > 0.05f)
            _facingSign = rb.linearVelocity.x >= 0f ? +1 : -1;

        if (useLedgeGuard && ledgeGuard)
            ledgeGuard.SetFacingSign(_lockFaceTillStunEnd ? _lockedFaceSign : _facingSign);

        // гравитация по Y, контроль по X
        _facingSign = (_chargeDir.x >= 0f) ? +1 : -1;
        _lastMoveSign = _facingSign;

        float targetX = _chargeDir.x * chargeSpeed;
        float newX = Mathf.MoveTowards(rb.linearVelocity.x, targetX, chargeAcceleration * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);

        if (_stateT < spinMinDuration) return;

        // [NEW] отскок от ИГРОКА в режиме Spin
        if (CheckPlayerHit(out var awayFromPlayer, out _))
        {
            StartArcedBounce(awayFromPlayer, BounceCause.Player);
            return;
        }

        // отскок от СТЕНЫ
        if (TryGetWallHit(out var wallNormal))
        {
            Vector2 t = new(-wallNormal.y, wallNormal.x);
            Vector2 alongSurface = t.normalized * Mathf.Sign(Vector2.Dot(t, -_chargeDir));
            _queuedSpinDir = -_lastMoveSign;
            StartArcedBounce(alongSurface, BounceCause.Wall);
        }
    }

    // — состояния/утилиты —
    void BeginWindup(Transform target)
    {
        _aggroLeft = aggroTimeout;                      // START агро
        _targetSnapshotPos = target ? (Vector2)target.position : (Vector2)(transform.position + Vector3.right);
        _hasSnapshot = true; Fire(hTriggerAttack); SwitchTo(State.Windup); if (patrol) patrol.enabled = false;
    }

    void BeginChargeFromSnapshot()
    {
        if (!_hasSnapshot) { SwitchTo(State.Patrol); return; }
        float sx = Mathf.Sign((_targetSnapshotPos - (Vector2)transform.position).x);
        if (Mathf.Approximately(sx, 0f)) sx = +1f;
        _chargeDir = new Vector2(sx, 0f); _facingSign = (_chargeDir.x >= 0f) ? +1 : -1; ApplyFlip(_facingSign);
        rb.linearVelocity = Vector2.zero; Fire(hSpinningTrig); SwitchTo(State.Spin);
    }

    void BeginWindupForcedTrigger(int nextDirSign)
    {
        nextDirSign = nextDirSign >= 0 ? +1 : -1;
        _lockFaceTillStunEnd = false;
        _facingSign = nextDirSign; ApplyFlip(_facingSign);
        _targetSnapshotPos = (Vector2)transform.position + Vector2.right * nextDirSign; _hasSnapshot = true;
        Fire(hTriggerAttack); SwitchTo(State.Windup); if (patrol) patrol.enabled = false;
    }

    void StartArcedBounce(Vector2 awayHint, BounceCause _)
    {
        Vector2 tangent = Vector2.right;
        float sign = Mathf.Sign(Vector2.Dot(tangent, awayHint));
        Vector2 dirHoriz = tangent * (sign == 0 ? -1f : sign);

        float g = Mathf.Abs(Physics2D.gravity.y * Mathf.Max(0f, rb.gravityScale));
        float H = Mathf.Max(0f, bounceArcHeight);
        float D = Mathf.Max(0f, bounceArcDistance);

        float vy0 = (g > 0f && H > 0f) ? Mathf.Sqrt(2f * g * H) : 0f;
        float T  = g > 0f ? (vy0 > 0f ? 2f * vy0 / g : Mathf.Max(0.06f, D / Mathf.Max(0.01f, chargeSpeed))) : 0.2f;
        float vx = (T > 0f) ? (D / T) : 0f;

        rb.linearVelocity = dirHoriz * vx + Vector2.up * vy0;
        SwitchTo(State.StunArc); rb.linearDamping = _savedDrag; _stunTimeLeft = stunStandDuration;
    }

    // [NEW] детект попадания по PlayerHurtbox во время Spin
    bool CheckPlayerHit(out Vector2 away, out Vector2 hitPoint)
    {
        away = Vector2.zero;
        hitPoint = transform.position;

        if (hitSources == null || hitSources.Length == 0) return false;
        if (playerHurtboxLayers.value == 0) return false;

        var filter = new ContactFilter2D { useLayerMask = true, layerMask = playerHurtboxLayers, useTriggers = true };

        foreach (var src in hitSources)
        {
            if (!src || !src.enabled) continue;
            int n = src.Overlap(filter, _colBuf);
            if (n <= 0) continue;

            Collider2D best = null; float bestD = float.PositiveInfinity;
            for (int k = 0; k < n; k++)
            {
                var other = _colBuf[k];
                if (!other) continue;
                float d = (other.transform.position - transform.position).sqrMagnitude;
                if (d < bestD) { bestD = d; best = other; }
            }

            if (best != null)
            {
                Vector2 p = best.bounds.ClosestPoint(transform.position);
                hitPoint = p;
                away = (Vector2)transform.position - p;
                if (away.sqrMagnitude < 1e-4f) away = -_chargeDir;
                return true;
            }
        }
        return false;
    }

    bool TryGetWallHit(out Vector2 normal)
    {
        normal = Vector2.zero; if (!bodyCollider || solidLayers.value==0) return false;
        Vector2 v = rb.linearVelocity; float spd=v.magnitude; if (spd<0.05f) return false; Vector2 dir=v/spd;
        var filter = new ContactFilter2D { useLayerMask=true, layerMask=solidLayers, useTriggers=false };

        int c = rb.GetContacts(filter, _cpBuf);
        for (int i=0;i<c;i++){var n=_cpBuf[i].normal; if (Mathf.Abs(n.x)>=MIN_HORIZ && Vector2.Dot(n,dir)<=-MIN_FACING){ normal=n; return true; }}

        float castDist = Mathf.Max(0.03f, spd*Time.fixedDeltaTime*1.25f);
        int h = bodyCollider.Cast(dir, filter, _rhBuf, castDist);
        for (int i=0;i<h;i++){var n=_rhBuf[i].normal; if (Mathf.Abs(n.x)>=MIN_HORIZ && Vector2.Dot(n,dir)<=-MIN_FACING){ normal=n; return true; }}
        return false;
    }

    bool IsGrounded()
    {
        if (!bodyCollider || solidLayers.value==0) return false;
        var filter = new ContactFilter2D { useLayerMask=true, layerMask=solidLayers, useTriggers=false };
        int c = rb.GetContacts(filter, _cpBuf); for (int i=0;i<c;i++) if (_cpBuf[i].normal.y>=MIN_GROUND) return true;
        int h = bodyCollider.Cast(Vector2.down, filter, _rhBuf, 0.05f); for (int i=0;i<h;i++) if (_rhBuf[i].normal.y>=MIN_GROUND) return true;
        return false;
    }

    int SignFromScale(Transform t){ float sx=t? t.localScale.x:1f; if (Mathf.Approximately(sx,0f)) sx=1f; return sx>=0f?+1:-1; }
    int DirTo(Transform t){ if(!t) return _facingSign; float sx=Mathf.Sign(t.position.x-transform.position.x); return sx==0? _facingSign : (sx>0?+1:-1); }
    void ApplyFlip(int sign){ if(!flipByScaleX||!facingTransform) return; var s=facingTransform.localScale; float ax=Mathf.Abs(s.x); if(ax<=0f) ax=1f; s.x=ax*(sign>=0?1:-1); facingTransform.localScale=s; }

    void SwitchTo(State s, bool force=false)
    {
        if (!force && _state==s) return;
        _state=s; _stateT=0f;
        if (mirror){ if(s==State.Patrol) mirror.SetPhase(EnemyStateMirror.Phase.Patrol);
                     else if(s==State.Windup) mirror.SetPhase(EnemyStateMirror.Phase.Windup);
                     else if(s==State.Spin) mirror.SetPhase(EnemyStateMirror.Phase.Spin);
                     else mirror.SetPhase(EnemyStateMirror.Phase.Stun); }

        if (s==State.Patrol){ if (patrol) patrol.enabled=true; _hasSnapshot=false; _lockFaceTillStunEnd=false; _queuedSpinDir=0; rb.linearDamping=_savedDrag; Fire(hPatrolTrigger); }
        else { if (patrol) patrol.enabled=false; rb.linearDamping = (s==State.StunHold)?stunHoldLinearDrag:_savedDrag; }
    }

    void Fire(int hash){ if (!animator) return; if (hash==hPatrolTrigger && _state!=State.Patrol) return; animator.SetTrigger(hash); }
}
