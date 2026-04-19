using UnityEngine;

/// <summary>
/// Drives a <see cref="LineRenderer"/> so it always connects the "head" and "body"
/// transforms of the ReworkBugEnemyGround. The renderer is expected to have its
/// useWorldSpace flag enabled — we feed it world-space positions every LateUpdate.
///
/// Optionally fades/hides the line when the enemy dies so the dissolve VFX looks clean.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class ReworkHeadBodyTether : MonoBehaviour
{
    [Header("Endpoints")]
    [SerializeField] private Transform headAnchor;
    [SerializeField] private Transform bodyAnchor;

    [Header("Optional")]
    [SerializeField] private LineRenderer lineRenderer;
    [Tooltip("If set, the line is disabled when the enemy is no longer alive.")]
    [SerializeField] private Health health;
    [SerializeField] private bool hideOnDeath = true;

    private void Reset()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    private void Awake()
    {
        if (!lineRenderer) lineRenderer = GetComponent<LineRenderer>();

        if (lineRenderer)
        {
            lineRenderer.useWorldSpace = true;
            lineRenderer.positionCount = 2;
        }

        if (!health)
        {
            health = GetComponent<Health>();
            if (!health) health = GetComponentInParent<Health>();
        }
    }

    private void LateUpdate()
    {
        if (!lineRenderer || !headAnchor || !bodyAnchor)
            return;

        if (hideOnDeath && health != null && !health.IsAlive)
        {
            if (lineRenderer.enabled) lineRenderer.enabled = false;
            return;
        }

        if (!lineRenderer.enabled) lineRenderer.enabled = true;

        lineRenderer.SetPosition(0, headAnchor.position);
        lineRenderer.SetPosition(1, bodyAnchor.position);
    }
}
