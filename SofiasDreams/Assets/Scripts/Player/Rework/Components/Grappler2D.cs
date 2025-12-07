using System.Collections;
using UnityEngine;
using Zenject;

[DisallowMultipleComponent]
public class Grappler2D : MonoBehaviour, IGrappler
{
    [Header("Refs")]
    [SerializeField] Rigidbody2D rb;

    GrappleSettings _s;
    bool _configured;

    IMobilityGate _gate;
    IMover        _mover;
    SignalBus     _bus;

    bool      _isGrappling;
    float     _cooldownUntil;
    Vector2   _savedTravelDir = Vector2.up;
    float     _savedEntrySpeed;
    float     _originalGravity;
    bool      _gravityOverridden;
    Coroutine _routine;

    public bool IsGrappling => _isGrappling;

    [Inject]
    void Inject(IMobilityGate gate, IMover mover, SignalBus bus)
    {
        _gate  = gate;
        _mover = mover;
        _bus   = bus;
    }

    public void Configure(GrappleSettings settings)
    {
        _s = settings;
        _configured = true;
    }

    void Reset()
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
    }

    void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
    }

    void OnEnable()
    {
        _bus?.Subscribe<GrappleCommand>(OnGrappleCommand);
    }

    void OnDisable()
    {
        _bus?.Unsubscribe<GrappleCommand>(OnGrappleCommand);
    }

    void OnGrappleCommand(GrappleCommand _)
    {
        if (!_configured || rb == null) return;
        if (_isGrappling) return;
        if (Time.time < _cooldownUntil) return;

        if (!TryFindTarget(out var target))
        {
            _bus.Fire(new GrappleFinished { interrupted = true });
            return;
        }

        // flip immediately toward grapple point ---
        if (_mover != null)
        {
            Vector2 origin   = rb ? rb.position : (Vector2)transform.position;
            Vector2 toTarget = target - origin;

            if (Mathf.Abs(toTarget.x) > 0.01f)
            {
                int desiredDir = toTarget.x > 0f ? 1 : -1;
                _mover.ForceFacing(desiredDir);
            }
        }
        
        _cooldownUntil = Time.time + _s.cooldown;
        _routine = StartCoroutine(GrappleRoutine(target));
    }

    bool TryFindTarget(out Vector2 bestTarget)
    {
        bestTarget = Vector2.zero;

        Vector2 origin = rb ? rb.position : (Vector2)transform.position;

        var hits = Physics2D.OverlapCircleAll(origin, _s.radius, _s.grappleLayer);
        if (hits == null || hits.Length == 0) return false;

        float bestDistSq = float.PositiveInfinity;

        foreach (var h in hits)
        {
            Vector2 p   = h.transform.position;
            Vector2 toP = p - origin;
            float distSq = toP.sqrMagnitude;
            if (distSq < 1e-6f) continue;

            Vector2 dir = toP.normalized;

            // half-circle above
            if (Vector2.Dot(dir, Vector2.up) < 0f)
                continue;

            // obstacle check
            if (Physics2D.Linecast(origin, p, _s.obstacleLayer))
                continue;

            if (distSq < bestDistSq)
            {
                bestDistSq = distSq;
                bestTarget = p;
            }
        }

        return bestDistSq < float.PositiveInfinity;
    }

    IEnumerator GrappleRoutine(Vector2 target)
    {
        _isGrappling = true;

        _gate?.BlockMovement(MobilityBlockReason.Grapple);
        _gate?.BlockJump(MobilityBlockReason.Grapple);

        // Tell animator we started grappling (will play frames 1–2 then 3)
        _bus.Fire(new GrappleStarted { point = target });
        
        _originalGravity   = rb.gravityScale;
        _gravityOverridden = false;

        if (_s.zeroGravityWhileGrappling)
        {
            rb.gravityScale   = 0f;
            _gravityOverridden = true;
        }

        // Save entry speed BEFORE we freeze for startup
        _savedEntrySpeed = rb.linearVelocity.magnitude;

        // Freeze velocity during anticipation so player doesn’t slide
        rb.linearVelocity = Vector2.zero;

        // --- Startup pause for frames 1–2 ---
        if (_s.startupDelay > 0f)
        {
            float t = 0f;
            while (t < _s.startupDelay)
            {
                t += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }
        }
        // ------------------------------------

        Vector2 startPos       = rb.position;
        Vector2 toTargetStart  = target - startPos;
        
        _savedTravelDir = toTargetStart.sqrMagnitude > 1e-6f
            ? toTargetStart.normalized
            : Vector2.up;

        rb.linearVelocity = Vector2.zero;

        float stopSqr = Mathf.Max(0.01f, _s.stopDistance);
        stopSqr *= stopSqr;
        float dt = Time.fixedDeltaTime;

        // zip
        while (true)
        {
            Vector2 pos = rb.position;
            Vector2 to  = target - pos;
            float d2    = to.sqrMagnitude;
            if (d2 <= stopSqr) break;

            Vector2 dir  = to.normalized;
            float step   = _s.moveSpeed * dt;
            float dist   = Mathf.Sqrt(d2);
            if (step > dist) step = dist;

            rb.MovePosition(pos + dir * step);
            yield return new WaitForFixedUpdate();
        }

        // arrival → exit
        {
            _isGrappling = false;
            _bus.Fire(new GrappleFinished { interrupted = false });
            
            Vector2 pos = rb.position;
            Vector2 to  = target - pos;
            Vector2 dir = to.sqrMagnitude > 1e-6f ? to.normalized : _savedTravelDir;
            Vector2 desired = target - dir * Mathf.Max(_s.arrivalClearance, 0f);

            rb.MovePosition(desired);

            if (_gravityOverridden)
            {
                rb.gravityScale    = _originalGravity;
                _gravityOverridden = false;
            }

            float   carry    = _savedEntrySpeed * Mathf.Clamp01(_s.carryOverEntrySpeedFactor);
            float   strength = _s.exitStrength + carry;
            Vector2 rawExitVel = _savedTravelDir * strength;

            float maxX = Mathf.Max(0f, _s.maxExitSpeedX);
            float maxY = Mathf.Max(0f, _s.maxExitSpeedY);
            float clampedX = maxX > 0f ? Mathf.Clamp(rawExitVel.x, -maxX, maxX) : rawExitVel.x;
            float clampedY = maxY > 0f ? Mathf.Clamp(rawExitVel.y, -maxY, maxY) : rawExitVel.y;

            Vector2 targetExitVel = new Vector2(clampedX, clampedY);

            Vector2 startVel = rb.linearVelocity;
            if (startVel.y < 0f) startVel.y = 0f;
            rb.linearVelocity = startVel;

            if (_mover != null)
            {
                float hard = Mathf.Max(_s.hardLockDuration, _s.exitBlendTime);
                float soft = Mathf.Max(hard, _s.softCarryMaxDuration);

                _mover.SetExternalVelocity(
                    targetExitVel,
                    hard,
                    soft,
                    overrideX: true,
                    overrideY: false);
            }

            float dur = Mathf.Max(0f, _s.exitBlendTime);

            if (dur <= 0f)
            {
                rb.linearVelocity = targetExitVel;
            }
            else if (_s.blendByVelocityLerp)
            {
                float t = 0f;
                while (t < dur)
                {
                    t += Time.fixedDeltaTime;
                    float a = t / dur;
                    a = a * a * (3f - 2f * a);

                    Vector2 v = Vector2.Lerp(startVel, targetExitVel, a);
                    if (maxX > 0f) v.x = Mathf.Clamp(v.x, -maxX, maxX);
                    if (maxY > 0f) v.y = Mathf.Clamp(v.y, -maxY, maxY);

                    rb.linearVelocity = v;
                    yield return new WaitForFixedUpdate();
                }

                rb.linearVelocity = targetExitVel;
            }
            else
            {
                float t = 0f;
                while (t < dur)
                {
                    t += Time.fixedDeltaTime;
                    float a = Mathf.Clamp01(t / dur);
                    a = a * a * (3f - 2f * a);

                    Vector2 desiredVel = Vector2.Lerp(startVel, targetExitVel, a);
                    if (maxX > 0f) desiredVel.x = Mathf.Clamp(desiredVel.x, -maxX, maxX);
                    if (maxY > 0f) desiredVel.y = Mathf.Clamp(desiredVel.y, -maxY, maxY);

                    Vector2 deltaVel = desiredVel - rb.linearVelocity;
                    rb.AddForce(deltaVel * rb.mass, ForceMode2D.Impulse);
                    yield return new WaitForFixedUpdate();
                }

                rb.linearVelocity = targetExitVel;
            }
        }

        float keepOff = Mathf.Max(_s.hardLockDuration, _s.exitBlendTime);
        float guard   = 0f;
        while (guard < keepOff)
        {
            guard += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        _gate?.UnblockMovement(MobilityBlockReason.Grapple);
        _gate?.UnblockJump(MobilityBlockReason.Grapple);

        
        _routine = null;
    }

    public void CancelGrapple()
    {
        if (!_isGrappling) return;

        if (_routine != null)
            StopCoroutine(_routine);

        if (_gravityOverridden)
        {
            rb.gravityScale    = _originalGravity;
            _gravityOverridden = false;
        }

        _gate?.UnblockMovement(MobilityBlockReason.Grapple);
        _gate?.UnblockJump(MobilityBlockReason.Grapple);

        _isGrappling = false;
        _bus.Fire(new GrappleFinished { interrupted = true });
        _routine = null;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!rb)
            rb = GetComponent<Rigidbody2D>();

        float radius = _configured && _s.radius > 0f ? _s.radius : 3f;
        Vector3 c = Application.isPlaying && rb ? (Vector3)rb.position : transform.position;

        Gizmos.color = new Color(0.2f, 0.75f, 1f, 0.6f);
        DrawHalfCircle(c, radius, 32);
    }

    void DrawHalfCircle(Vector3 center, float r, int segs)
    {
        Vector3 prev = center + Quaternion.Euler(0, 0, -90f) * Vector3.up * r;
        for (int i = 1; i <= segs; i++)
        {
            float t = Mathf.Lerp(-90f, 90f, i / (float)segs);
            Vector3 next = center + Quaternion.Euler(0, 0, t) * Vector3.up * r;
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
#endif
}
