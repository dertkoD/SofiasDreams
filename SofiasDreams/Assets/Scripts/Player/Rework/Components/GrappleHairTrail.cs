using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class GrappleHairTrailSprites : MonoBehaviour, IInitializable, System.IDisposable
{
    [Header("Refs")]
    [SerializeField] Transform      playerRoot;
    [SerializeField] SpriteRenderer segmentPrefab;

    [Header("Layout")]
    [Tooltip("World distance between centers of consecutive hair segments.")]
    [SerializeField] float segmentWorldLength = 0.4f;

    [Tooltip("Maximum number of segments to allocate in the pool.")]
    [SerializeField] int maxSegments = 32;

    [Header("Grow Phase (wind-up)")]
    [Tooltip("Time it takes for the hair to grow from the player to the target.")]
    [SerializeField] float growDuration = 0.12f;

    SignalBus _bus;

    bool   _active;
    bool   _inGrowPhase;
    float  _growElapsed;

    Vector2 _startWorld;    // player position at grapple start
    Vector2 _targetWorld;   // grapple point

    // Simple pool of sprite segments
    readonly List<SpriteRenderer> _segments = new();

    [Inject]
    void Construct(SignalBus bus)
    {
        _bus = bus;
    }

    void Awake()
    {
        if (!playerRoot) playerRoot = transform;

        if (!segmentPrefab)
        {
            Debug.LogError("[HairTrailSprites] Segment prefab not assigned.", this);
            enabled = false;
            return;
        }

        // Pre-allocate pool
        for (int i = 0; i < maxSegments; i++)
        {
            var seg = Instantiate(segmentPrefab, transform);
            seg.gameObject.SetActive(false);
            _segments.Add(seg);
        }
    }

    public void Initialize()
    {
        _bus.Subscribe<GrappleStarted>(OnGrappleStarted);
        _bus.Subscribe<GrappleFinished>(OnGrappleFinished);
    }

    public void Dispose()
    {
        _bus.Unsubscribe<GrappleStarted>(OnGrappleStarted);
        _bus.Unsubscribe<GrappleFinished>(OnGrappleFinished);
    }

    void OnGrappleStarted(GrappleStarted s)
    {
        _startWorld  = playerRoot.position;
        _targetWorld = s.point;

        _growElapsed = 0f;
        _inGrowPhase = true;
        _active      = true;

        SetAllSegmentsActive(false);
    }

    void OnGrappleFinished(GrappleFinished s)
    {
        _active      = false;
        _inGrowPhase = false;
        SetAllSegmentsActive(false);
    }

    void LateUpdate()
    {
        if (!_active) return;

        Vector2 start = _startWorld;
        Vector2 end   = _targetWorld;

        float totalLen = Vector2.Distance(start, end);
        if (totalLen < 0.001f)
        {
            SetAllSegmentsActive(false);
            return;
        }

        Vector2 visibleStart;
        Vector2 visibleEnd;

        if (_inGrowPhase)
        {
            // Phase 1: hair grows from player -> target during wind-up
            _growElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_growElapsed / Mathf.Max(growDuration, 0.01f));

            visibleStart = start;
            visibleEnd   = Vector2.Lerp(start, end, t);

            if (t >= 1f)
                _inGrowPhase = false;
        }
        else
        {
            // Phase 2: zip – hair remains only ahead of the player
            Vector2 playerPos = playerRoot.position;
            Vector2 dir       = end - start;
            float   length    = dir.magnitude;
            if (length < 0.001f)
            {
                SetAllSegmentsActive(false);
                return;
            }

            Vector2 dirN = dir / length;

            float proj = Vector2.Dot(playerPos - start, dirN);
            float t    = Mathf.Clamp01(proj / length);

            visibleStart = Vector2.Lerp(start, end, t);
            visibleEnd   = end;
        }

        float visibleLen = Vector2.Distance(visibleStart, visibleEnd);
        if (visibleLen < 0.001f)
        {
            SetAllSegmentsActive(false);
            return;
        }

        // Debug line so you still see the path in Scene view
        Debug.DrawLine(visibleStart, visibleEnd, Color.magenta);

        // Place sprites along [visibleStart, visibleEnd]
        Vector2 segmentDir = (visibleEnd - visibleStart).normalized;
        int needed = Mathf.Min(
            maxSegments,
            Mathf.CeilToInt(visibleLen / Mathf.Max(segmentWorldLength, 0.01f))
        );

        float z = transform.position.z;

        for (int i = 0; i < _segments.Count; i++)
        {
            bool enable = i < needed;
            var seg = _segments[i];

            if (!enable)
            {
                if (seg.gameObject.activeSelf)
                    seg.gameObject.SetActive(false);
                continue;
            }

            // Position each segment along the line, centered in its slice
            float t = (i + 0.5f) / Mathf.Max(needed, 1);
            Vector2 pos2D = Vector2.Lerp(visibleStart, visibleEnd, t);

            seg.transform.position = new Vector3(pos2D.x, pos2D.y, z);

            // Rotate to face along the line
            float angle = Mathf.Atan2(segmentDir.y, segmentDir.x) * Mathf.Rad2Deg;
            seg.transform.rotation = Quaternion.Euler(0f, 0f, angle);

            if (!seg.gameObject.activeSelf)
                seg.gameObject.SetActive(true);
        }
    }

    void SetAllSegmentsActive(bool active)
    {
        foreach (var seg in _segments)
        {
            if (seg && seg.gameObject.activeSelf != active)
                seg.gameObject.SetActive(active);
        }
    }
}
