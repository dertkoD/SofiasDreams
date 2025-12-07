using UnityEngine;
using System.Collections;

public class GrappleSystem : MonoBehaviour
{
    [Header("Targeting")]
    public float radius = 8f;
    public LayerMask grappleLayer;
    public LayerMask obstacleLayer;

    [Header("Zip (straight-line)")]
    [Tooltip("Constant travel speed toward the hook.")]
    public float moveSpeed = 27f;
    [Tooltip("Stop when within this distance of the target to avoid contact jitter.")]
    public float stopDistance = 0f;
    [Tooltip("Stand off from the point by this much to avoid overlap on arrival.")]
    public float arrivalClearance = 0f;
    [Tooltip("Set true to null gravity during zip for a perfect straight line.")]
    public bool zeroGravityWhileGrappling = true;

    [Header("Exit Jump (fixed strength, direction = travel vector)")]
    [Tooltip("Total exit speed magnitude. Direction = saved travel dir (start->target).")]
    public float exitStrength = 16f;
    [Tooltip("Optionally add some of the entry speed to the exit (0 = none).")]
    [Range(0f, 1f)] public float carryOverEntrySpeedFactor = 0f;

    [Header("Exit Limits (per-axis)")]
    [Tooltip("Max horizontal exit speed. 0 or negative = no horizontal clamp.")]
    public float maxExitSpeedX = 11f;
    [Tooltip("Max vertical exit speed. 0 or negative = no vertical clamp.")]
    public float maxExitSpeedY = 16f;

    [Header("Exit Smoothing")]
    [Tooltip("Blend time from zip velocity into exit velocity (seconds). 0 = instant.")]
    public float exitBlendTime = 0.05f;
    [Tooltip("If true, lerp velocity over exitBlendTime; if false, ramp by impulses.")]
    public bool blendByVelocityLerp = true;

    [Header("Momentum Windows")]
    [Tooltip("Hard lock: duration where exit velocity is enforced every frame.")]
    public float hardLockDuration = 0.12f;
    [Tooltip("Soft carry: max time momentum is preserved until steer/ground/timeout.")]
    public float softCarryMaxDuration = 0.50f;

    [Header("Timing / Control")]
    public KeyCode grappleKey = KeyCode.G;
    public float cooldown = 0.1f;
    public Behaviour[] disableWhileGrappling;

    // ----- Private -----
    private Rigidbody2D _rb;
    private bool _isGrappling;
    private float _cooldownUntil;

    // Saved for exit
    private Vector2 _savedTravelDir = Vector2.up;  // normalized
    private float _savedEntrySpeed = 0f;

    void Awake() => _rb = GetComponent<Rigidbody2D>();

    // Call this from your PlayerController.Update()
    public void HandleGrappling()
    {
        if (_isGrappling) return;
        if (Time.time < _cooldownUntil) return;
        if (!Input.GetKeyDown(grappleKey)) return;

        if (TryFindTarget(out Vector2 target))
            StartCoroutine(GrappleRoutine(target));
    }

    private bool TryFindTarget(out Vector2 bestTarget)
    {
        bestTarget = Vector2.zero;
        Vector2 origin = _rb.position;

        var hits = Physics2D.OverlapCircleAll(origin, radius, grappleLayer);
        if (hits == null || hits.Length == 0) return false;

        float best = float.PositiveInfinity;
        foreach (var h in hits)
        {
            Vector2 p = h.transform.position;
            Vector2 toP = p - origin;

            // Only allow targets in the half-circle above
            if (Vector2.Dot(toP.normalized, Vector2.up) < 0f) continue;

            // Blocked?
            if (Physics2D.Linecast(origin, p, obstacleLayer)) continue;

            float d = toP.sqrMagnitude;
            if (d < best) { best = d; bestTarget = p; }
        }
        return best < float.PositiveInfinity;
    }

    private IEnumerator GrappleRoutine(Vector2 target)
    {
        _isGrappling = true;
        _cooldownUntil = Time.time + cooldown;

        // Disable player-driven behaviours while grappling
        if (disableWhileGrappling != null)
            foreach (var b in disableWhileGrappling) if (b) b.enabled = false;

        float originalGravity = _rb.gravityScale;
        if (zeroGravityWhileGrappling) _rb.gravityScale = 0f;

        _savedEntrySpeed = _rb.linearVelocity.magnitude;

        // Lock direction from current position -> target (determines exit direction)
        Vector2 startPos = _rb.position;
        Vector2 toTargetStart = target - startPos;
        _savedTravelDir = toTargetStart.sqrMagnitude > 1e-6f ? toTargetStart.normalized : Vector2.up;

        // Clear velocity so nothing fights the zip
        _rb.linearVelocity = Vector2.zero;

        // Straight-line MovePosition with overshoot clamp (prevents oscillation/twitch)
        float stopSqr = Mathf.Max(0.01f, stopDistance);
        stopSqr *= stopSqr;
        float dt = Time.fixedDeltaTime;

        while (true)
        {
            Vector2 pos = _rb.position;
            Vector2 to = target - pos;
            float d2 = to.sqrMagnitude;
            if (d2 <= stopSqr) break;

            Vector2 dir = to.normalized;
            float step = moveSpeed * dt;
            float dist = Mathf.Sqrt(d2);
            if (step > dist) step = dist;

            _rb.MovePosition(pos + dir * step);
            yield return new WaitForFixedUpdate();
        }

        // === Seamless arrival -> exit (smoothed, no pause) ===
        {
            Vector2 pos = _rb.position;
            Vector2 to = target - pos;
            Vector2 dir = to.sqrMagnitude > 1e-6f ? to.normalized : _savedTravelDir;
            Vector2 desired = target - dir * Mathf.Max(arrivalClearance, 0f);

            // Move to standoff in this same step (no pause)
            _rb.MovePosition(desired);

            // Restore gravity before shaping exit
            if (zeroGravityWhileGrappling) _rb.gravityScale = originalGravity;

            // Compute raw exit vector from direction & strength (+ optional carry)
            float carry = _savedEntrySpeed * Mathf.Clamp01(carryOverEntrySpeedFactor);
            float strength = exitStrength + carry;
            Vector2 rawExitVel = _savedTravelDir * strength;

            // ----- PER-AXIS CLAMP -----
            float maxX = Mathf.Max(0f, maxExitSpeedX);
            float maxY = Mathf.Max(0f, maxExitSpeedY);

            float clampedX = (maxX > 0f) ? Mathf.Clamp(rawExitVel.x, -maxX, maxX) : rawExitVel.x;
            float clampedY = (maxY > 0f) ? Mathf.Clamp(rawExitVel.y, -maxY, maxY) : rawExitVel.y;

            Vector2 targetExitVel = new Vector2(clampedX, clampedY);
            // ---------------------------

            // Start from current velocity (usually tiny after zip), clamp downward Y away
            Vector2 startVel = _rb.linearVelocity;
            if (startVel.y < 0f) startVel.y = 0f;
            _rb.linearVelocity = startVel;

            // Tell the movement script to protect X during blend + hard lock, then soft-carry
            if (TryGetComponent<PlayerMovement>(out var mover))
            {
                float hard = Mathf.Max(hardLockDuration, exitBlendTime); // ensure blend is protected
                float soft = Mathf.Max(hard, softCarryMaxDuration);
                mover.SetExternalVelocity(targetExitVel, hard, soft, overrideX: true, overrideY: false);
            }

            // Blend to targetExitVel over exitBlendTime
            float dur = Mathf.Max(0f, exitBlendTime);
            if (dur <= 0f)
            {
                // Instant exit (still clamped)
                _rb.linearVelocity = targetExitVel;
            }
            else if (blendByVelocityLerp)
            {
                // Smooth velocity lerp (very clean, minimal pop)
                float t = 0f;
                while (t < dur)
                {
                    t += Time.fixedDeltaTime;
                    float a = t / dur;
                    // smoothstep easing
                    a = a * a * (3f - 2f * a);
                    Vector2 v = Vector2.Lerp(startVel, targetExitVel, a);

                    // enforce per-axis limits during blend too
                    if (maxX > 0f) v.x = Mathf.Clamp(v.x, -maxX, maxX);
                    if (maxY > 0f) v.y = Mathf.Clamp(v.y, -maxY, maxY);

                    _rb.linearVelocity = v;
                    yield return new WaitForFixedUpdate();
                }
                _rb.linearVelocity = targetExitVel;
            }
            else
            {
                // Force ramp (physically adds up; weightier feel)
                float t = 0f;
                while (t < dur)
                {
                    t += Time.fixedDeltaTime;
                    float a = Mathf.Clamp01(t / dur);
                    a = a * a * (3f - 2f * a); // smoothstep

                    Vector2 desiredVel = Vector2.Lerp(startVel, targetExitVel, a);

                    // enforce per-axis limits
                    if (maxX > 0f) desiredVel.x = Mathf.Clamp(desiredVel.x, -maxX, maxX);
                    if (maxY > 0f) desiredVel.y = Mathf.Clamp(desiredVel.y, -maxY, maxY);

                    Vector2 deltaVel = desiredVel - _rb.linearVelocity;
                    _rb.AddForce(deltaVel * _rb.mass, ForceMode2D.Impulse);
                    yield return new WaitForFixedUpdate();
                }
                _rb.linearVelocity = targetExitVel;
            }
        }

        // Keep behaviours disabled through the hard lock so nothing stomps the launch
        float keepOff = Mathf.Max(hardLockDuration, exitBlendTime);
        float tGuard = 0f;
        while (tGuard < keepOff)
        {
            tGuard += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        if (disableWhileGrappling != null)
            foreach (var b in disableWhileGrappling) if (b) b.enabled = true;

        _isGrappling = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.75f, 1f, 0.6f);
        Vector3 c = (_rb != null && Application.isPlaying) ? (Vector3)_rb.position : transform.position;
        DrawHalfCircle(c, radius, 32);
    }

    private void DrawHalfCircle(Vector3 center, float r, int segs)
    {
        Vector3 prev = center + Quaternion.Euler(0,0,-90f) * Vector3.up * r;
        for (int i=1;i<=segs;i++)
        {
            float t = Mathf.Lerp(-90f, 90f, i/(float)segs);
            Vector3 next = center + Quaternion.Euler(0,0,t) * Vector3.up * r;
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
}
