using UnityEngine;
using UnityEngine.VFX;

/// <summary>
/// Feeds the Smoke VFX graph with world-space head/body positions so the
/// "Position (Line)" block spawns particles along the tether, and pushes a
/// per-frame <c>ParticleVelocity</c> aligned with the head → body direction
/// so they actually travel along the line.
///
/// Expected exposed properties on the VFX asset (all present in SmokeVFX.vfx):
///   - Vector3 <c>StartPos</c>      (world-space)
///   - Vector3 <c>EndPos</c>        (world-space)
///   - Vector3 <c>ParticleVelocity</c>   (driven every frame)
///   - Float   <c>SpawnAmount</c>        (optional, scaled by line length)
///
/// Keep this component on an object that is NOT a child of the moving head —
/// the VFX Line shape must be in World space for this to look right.
/// </summary>
[DisallowMultipleComponent]
public class SmokeTetherController : MonoBehaviour
{
    [Header("Anchors")]
    [SerializeField] private Transform headAnchor;
    [SerializeField] private Transform bodyAnchor;

    [Header("VFX")]
    [SerializeField] private VisualEffect vfx;

    [Header("Tuning")]
    [Tooltip("How many particles per second, per 1 unit of line length. " +
             "Scales the SpawnAmount property proportionally to the current distance.")]
    [SerializeField, Min(0f)] private float particlesPerUnit = 25f;

    [Tooltip("Base travel speed (units/second) pushed into ParticleVelocity " +
             "along the head -> body direction.")]
    [SerializeField] private float travelSpeed = 3f;

    [Tooltip("When > 0 the controller also writes a particle lifetime hint " +
             "(not required by the graph, only used if you exposed LifetimeFactor).")]
    [SerializeField, Min(0f)] private float lifetimeFactor = 0.35f;

    [Tooltip("Optional: reverse so particles travel from body back to head.")]
    [SerializeField] private bool reverseDirection;

    static readonly int StartPosID        = Shader.PropertyToID("StartPos");
    static readonly int EndPosID          = Shader.PropertyToID("EndPos");
    static readonly int ParticleVelocityID = Shader.PropertyToID("ParticleVelocity");
    static readonly int SpawnAmountID     = Shader.PropertyToID("SpawnAmount");
    static readonly int LifetimeFactorID  = Shader.PropertyToID("LifetimeFactor");

    void Reset()
    {
        vfx = GetComponent<VisualEffect>();
    }

    void Awake()
    {
        if (!vfx) vfx = GetComponent<VisualEffect>();
    }

    void LateUpdate()
    {
        if (!vfx || !headAnchor || !bodyAnchor)
            return;

        Vector3 head = headAnchor.position;
        Vector3 body = bodyAnchor.position;

        Vector3 delta = body - head;
        float   dist  = delta.magnitude;

        Vector3 from = reverseDirection ? body : head;
        Vector3 to   = reverseDirection ? head : body;

        if (vfx.HasVector3(StartPosID)) vfx.SetVector3(StartPosID, from);
        if (vfx.HasVector3(EndPosID))   vfx.SetVector3(EndPosID,   to);

        if (vfx.HasVector3(ParticleVelocityID))
        {
            Vector3 dir = dist > 1e-5f ? (to - from) / dist : Vector3.zero;
            vfx.SetVector3(ParticleVelocityID, dir * travelSpeed);
        }

        if (vfx.HasFloat(SpawnAmountID))
            vfx.SetFloat(SpawnAmountID, Mathf.Max(0f, dist * particlesPerUnit));

        if (lifetimeFactor > 0f && vfx.HasFloat(LifetimeFactorID))
            vfx.SetFloat(LifetimeFactorID, lifetimeFactor);
    }
}
