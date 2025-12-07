using UnityEngine;

public class EnemyBrain2D : MonoBehaviour, IExternalHitStunHost
{
    [Header("Components")]
    public VisionCone2D vision;
    public LookAt2D lookAt;
    [SerializeField] EnemyPatrolBinder2D binder;

    [Header("Animation")]
    [SerializeField] Animator animator;            // <-- NEW

    [Header("Patrol")]
    [Min(0f)] public float waypointTolerance = 0.2f;

    [Header("Alert / Memory")]
    [Min(0f)] public float memoryTime = 1.25f;
    [Min(0f)] public float lingerTime = 0.75f;

    [Header("Distance Keeping")]
    [Min(0f)] public float safeDistance = 6f;
    [Min(0f)] public float distanceHysteresis = 0.5f;

    FloatMovement2D mover;
    Transform _seenTarget;
    int _wpIndex;

    float _lastSeenAt = -999f;
    Vector2 _lastSeenPos;
    Vector2 _lastSeenDir;
    float _alertStartedAt = -999f;

    bool _alert;                                    // <-- NEW: текущее логическое состояние
    public bool AlertActive => _alert;              // <-- публичный геттер
    public bool externalHitStunActive;

    public bool ExternalHitStunActive
    {
        get => externalHitStunActive;
        set => externalHitStunActive = value;
    }
    
    void Reset()
    {
        mover  = GetComponent<FloatMovement2D>();
        vision = GetComponent<VisionCone2D>();
        lookAt = GetComponent<LookAt2D>();
        binder = GetComponent<EnemyPatrolBinder2D>();
        if (!animator) animator = GetComponentInChildren<Animator>(true); // <-- NEW
    }

    void OnValidate()
    {
        if (!mover)  mover  = GetComponent<FloatMovement2D>();
        if (!vision) vision = GetComponent<VisionCone2D>();
        if (!lookAt) lookAt = GetComponent<LookAt2D>();
        if (!binder) binder = GetComponent<EnemyPatrolBinder2D>();
        if (!animator) animator = GetComponentInChildren<Animator>(true); // <-- NEW
    }

    void Awake()
    {
        if (!mover)  mover  = GetComponent<FloatMovement2D>();
        if (!binder) binder = GetComponent<EnemyPatrolBinder2D>();
        if (!animator) animator = GetComponentInChildren<Animator>(true); // <-- NEW
        if (!mover)
        {
            Debug.LogError("[EnemyBrain2D] FloatMovement2D missing, disabling.", this);
            enabled = false;
        }
    }

    void Update()
    {
        if (externalHitStunActive) return; 
        // Обновляем факт текущего видения и память
        _seenTarget = null;
        if (vision != null && vision.TryGetClosestTarget(out var t))
        {
            _seenTarget   = t;
            _lastSeenAt   = Time.time;
            _lastSeenPos  = t.position;
            _lastSeenDir  = ((Vector2)t.position - (Vector2)transform.position).normalized;

            if (!_alert) _alertStartedAt = Time.time;
        }

        // NEW: детект смены состояния и пуляем триггеры
        bool newAlert = IsAlert();
        if (newAlert != _alert)
        {
            _alert = newAlert;
            if (animator)
            {
                if (_alert)
                {
                    animator.ResetTrigger("Idle");
                    animator.SetTrigger("Angry");   // → Angry
                }
                else
                {
                    animator.ResetTrigger("Angry");
                    animator.SetTrigger("Idle");    // → Idle
                }
            }
        }
    }

    void FixedUpdate()
    {
        if (externalHitStunActive) return;
        if (!mover) return;
        if (_alert) AlertTick();
        else        PatrolTick();
    }

    bool IsAlert()
    {
        bool seenRecently = (Time.time - _lastSeenAt) <= memoryTime;
        bool holdActive   = (Time.time - _alertStartedAt) <= lingerTime;
        return seenRecently || holdActive;
    }

    void AlertTick()
    {
        Vector2 toTarget;
        bool hasLiveTarget = _seenTarget != null;

        if (hasLiveTarget)
            toTarget = (Vector2)_seenTarget.position - (Vector2)transform.position;
        else
            toTarget = _lastSeenPos - (Vector2)transform.position;

        float dist = toTarget.magnitude;

        float low  = Mathf.Max(0f, safeDistance - distanceHysteresis);
        float high = safeDistance + distanceHysteresis;

        if (dist < low) mover.MoveInDirection(-toTarget, mover.fleeSpeed);
        else            mover.MoveInDirection(Vector2.zero, 0f);

        if (ShouldRotate())
        {
            if (hasLiveTarget && toTarget.sqrMagnitude > 0.0001f) lookAt?.SetFacing(toTarget);
            else if (_lastSeenDir.sqrMagnitude > 0.0001f)        lookAt?.SetFacing(_lastSeenDir);
        }
    }

    void PatrolTick()
    {
        var route = binder ? binder.BoundRoute : null;
        if (route == null || route.Count == 0)
        {
            mover.MoveInDirection(Vector2.zero, 0f);
            return;
        }

        Vector3 wp = route.GetWaypointWorld(_wpIndex);
        Vector2 to = wp - transform.position;

        float tol = Mathf.Max(waypointTolerance, route.arriveDistance);
        if (to.magnitude <= tol)
        {
            _wpIndex = route.NextIndexLoop(_wpIndex);
            wp = route.GetWaypointWorld(_wpIndex);
            to = wp - transform.position;
        }

        mover.MoveInDirection(to, mover.patrolSpeed);
        if (ShouldRotate()) lookAt?.SetFacing(to);
    }

    bool ShouldRotate()
    {
        if (vision == null) return true;
        return vision.fov < 359.9f;
    }

    // NEW: универсальный вызов при смерти
    public void PlayDeath()
    {
        if (!animator) return;
        // Если были злыми — кратко возвращаем в Idle, затем Death
        animator.SetTrigger("Idle");
        animator.SetTrigger("Death");
    }
}
