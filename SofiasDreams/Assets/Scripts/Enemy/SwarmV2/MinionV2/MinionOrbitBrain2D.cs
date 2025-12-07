using System.Collections.Generic;
using UnityEngine;

public class MinionOrbitBrain2D : MonoBehaviour
{
    public GuardTargetBinder2D binder;
    public OrbitPatrol2D orbit;
    public FloatMovement2D mover;
    public VisionCone2D vision;
    public LookAt2D lookAt;
    public MinionShooter2D shooter;

    Rigidbody2D _rb;
    SwarmMinionSpawner _spawner;

    bool _isAggressor;
    Transform _attackTarget;
    Transform _supportTarget;

    [Header("Attack")]
    [Min(0.1f)] public float attackDesiredDistance = 4f;
    [Min(0f)]   public float attackHysteresis = 0.6f;
    [Min(0.1f)] public float approachSpeed = 4f;
    [Min(0.1f)] public float backOffSpeed   = 4.5f;

    [Header("Support")]
    [Min(0.1f)] public float supportDesiredDistance = 6f;
    [Min(0.0f)] public float supportLateralOffset  = 2f;   
    [Min(0.1f)] public float supportApproachSpeed = 3.2f;
    [Min(0.1f)] public float supportBackOffSpeed   = 3.6f;
    [Min(0f)]   public float supportHysteresis     = 0.6f;
    

    [Header("Support fire")]
    [Min(0f)] public float supportFireIntervalMin = 1.2f;
    [Min(0f)] public float supportFireIntervalMax = 2.0f;

    float _nextSupportShotAt;

    public Transform CurrentAttackTarget => _attackTarget;
    
    static readonly Dictionary<int,int> _nextSideByTarget = new(); // +1, -1 чередуем
    int _assignedSide = 0;

    int AllocateSide(Transform t)
    {
        int key = t ? t.GetInstanceID() : 0;
        if (!_nextSideByTarget.TryGetValue(key, out int next)) next = +1; // первым — вверх
        _nextSideByTarget[key] = -next; // следующий — вниз
        return next;
    }

    void Reset()
    {
        binder  = GetComponent<GuardTargetBinder2D>();
        orbit   = GetComponent<OrbitPatrol2D>();
        mover   = GetComponent<FloatMovement2D>();
        vision  = GetComponent<VisionCone2D>();
        lookAt  = GetComponent<LookAt2D>();
        shooter = GetComponent<MinionShooter2D>();
    }

    void Awake()
    {
        if (!binder) binder = GetComponent<GuardTargetBinder2D>();
        if (!orbit)  orbit  = GetComponent<OrbitPatrol2D>();
        if (!mover)  mover  = GetComponent<FloatMovement2D>();
        if (!vision) vision = GetComponent<VisionCone2D>();
        if (!lookAt) lookAt = GetComponent<LookAt2D>();
        if (!shooter) shooter = GetComponent<MinionShooter2D>();
        _rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (_spawner == null && binder && binder.Target)
            _spawner = binder.Target.GetComponent<SwarmMinionSpawner>();

        // Если у спавнера уже есть агрессор и это не мы — всегда саппортим его цель/последнюю цель
        if (_spawner != null && _spawner.HasAggressor && !_isAggressor)
        {
            _supportTarget = _spawner.AggressorTarget ?? _spawner.FallbackTarget; // NEW
            return;
        }

        // Агрессора нет: пробуем стать им, используя либо свою видимость, либо последнюю цель спавнера
        Transform t = null;
        if (vision && vision.TryGetClosestTarget(out var seen)) t = seen;
        else if (_spawner && _spawner.FallbackTarget) t = _spawner.FallbackTarget; // NEW

        if (t != null && !_isAggressor && _spawner != null)
        {
            _spawner.TryClaimAggressor(this, t);
            _supportTarget = t; // пока ждём назначение, двигаемся как саппорт
        }
        else if (t == null)
        {
            _supportTarget = null;
        }
    }

    void FixedUpdate()
    {
        if (!mover || !orbit || binder?.Target == null) return;

        if (_isAggressor && _attackTarget != null)
        {
            AttackTick();
            return;
        }

        if (_spawner != null && _spawner.HasAggressor && !_isAggressor && (_spawner.AggressorTarget || _spawner.FallbackTarget))
        {
            _supportTarget = _spawner.AggressorTarget ?? _spawner.FallbackTarget;
            SupportTick();
            return;
        }

        if (_supportTarget != null)
        {
            SupportTick();
            return;
        }

        // орбита к хозяину
        Vector2 center  = binder.Target.position;
        Vector2 selfPos = transform.position;
        Vector2 selfVel = _rb ? _rb.linearVelocity : Vector2.zero;

        Vector2 desiredVel = orbit.GetDesiredVelocity(selfPos, selfVel, center);
        float speed = desiredVel.magnitude;
        mover.MoveInDirection(desiredVel, speed);

        if (lookAt && (!vision || vision.fov < 359.9f) && speed > 1e-3f)
            lookAt.SetFacing(desiredVel);
    }

    void AttackTick()
    {
        Vector2 to = (Vector2)_attackTarget.position - (Vector2)transform.position;
        float dist = to.magnitude;

        float low  = Mathf.Max(0.1f, attackDesiredDistance - attackHysteresis);
        float high = attackDesiredDistance + attackHysteresis;

        Vector2 dir; float spd;
        if (dist > high)       { dir = to;   spd = approachSpeed; }
        else if (dist < low)   { dir = -to;  spd = backOffSpeed;  }
        else                   { dir = Vector2.zero; spd = 0f; }

        mover.MoveInDirection(dir, spd);
        if (lookAt) lookAt.SetFacing(to);
        shooter?.TryFireAt(_attackTarget.position);
    }

    void SupportTick()
    {
        if (_supportTarget == null) return;

        // фиксируем слот на цель: +1 "вверх", -1 "вниз"
        if (_assignedSide == 0) _assignedSide = AllocateSide(_supportTarget);

        Vector2 playerPos = _supportTarget.position;
        Vector2 selfPos   = transform.position;

        Vector2 toPlayer    = playerPos - selfPos;
        Vector2 dirToPlayer = toPlayer.sqrMagnitude > 1e-6f ? toPlayer.normalized : Vector2.right;
        Vector2 perp        = new Vector2(-dirToPlayer.y, dirToPlayer.x);

        // единственная ручка: насколько «вбок» уезжают саппорты
        Vector2 desiredPos = playerPos
                             - dirToPlayer * supportDesiredDistance
                             + perp * (_assignedSide * supportLateralOffset);

        Vector2 toDesired = desiredPos - selfPos;
        float dist = toDesired.magnitude;

        float band = Mathf.Max(0.15f, supportHysteresis);
        float far  = band;
        float near = band * 0.5f;

        Vector2 moveDir; float spd;
        if (dist > far)       { moveDir = toDesired;  spd = supportApproachSpeed; }
        else if (dist < near) { moveDir = -toDesired; spd = supportBackOffSpeed;  }
        else                  { moveDir = Vector2.zero; spd = 0f; }

        mover.MoveInDirection(moveDir, spd);
        if (lookAt) lookAt.SetFacing(toPlayer);

        if (shooter && Time.time >= _nextSupportShotAt)
        {
            shooter.TryFireAt(playerPos);
            float interval = Random.Range(supportFireIntervalMin, supportFireIntervalMax);
            _nextSupportShotAt = Time.time + Mathf.Max(0.05f, interval);
        }
    }

    public void EnterAttackMode(Transform target)
    {
        _isAggressor = true;
        _attackTarget = target;
    }

    public void ExitAttackMode()
    {
        _isAggressor = false;
        _attackTarget = null;
    }

    void OnDisable() => ExitAttackMode();
}
