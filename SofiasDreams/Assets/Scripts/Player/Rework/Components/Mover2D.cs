using UnityEngine;
using Zenject;

public class Mover2D : MonoBehaviour, IMover
{
    [Header("Refs")]
    [SerializeField] Rigidbody2D rb;
    [SerializeField] Transform  flipRoot;

    [Header("Feel")]
    [SerializeField] float inputDeadzone = 0.05f;

    IMobilityGate _gate;
    MoveSettings  _s;

    float _inputX;
    bool  _localLocked;
    int   _dir = 1;

    public int FacingDir => _dir;

    public void Configure(MoveSettings s)
    {
        _s = s;

        if (!flipRoot)
            flipRoot = transform;

        var sc = flipRoot.localScale;
        sc.x = Mathf.Abs(sc.x) * _dir;
        flipRoot.localScale = sc;
    }

    [Inject]
    void Inject(IMobilityGate gate) => _gate = gate;

    public void SetInput(float x)
    {
        _inputX = Mathf.Clamp(x, -1f, 1f);

        if (Mathf.Abs(_inputX) > inputDeadzone)
        {
            int newDir = _inputX > 0 ? 1 : -1;
            if (newDir != _dir)
            {
                _dir = newDir;
                ApplyFlip();
            }
        }
    }

    public bool IsMovementLocked =>
        _localLocked || (_gate?.IsMovementBlocked ?? false);

    public void SetMovementLocked(bool v)
    {
        _localLocked = v;

        if (v && rb)
        {
            var vel = rb.linearVelocity;
            vel.x = 0f;
            rb.linearVelocity = vel;
        }
    }

    public void SetExternalVelocity(
        Vector2 velocity,
        float hardLockDuration,
        float softCarryDuration,
        bool overrideX,
        bool overrideY)
    {
        if (!rb) return;

        var v = rb.linearVelocity;

        if (overrideX)
        {
            v.x = velocity.x;

            // Update facing based on external X velocity
            if (Mathf.Abs(v.x) > 0.01f)
            {
                int newDir = v.x > 0 ? 1 : -1;
                if (newDir != _dir)
                {
                    _dir = newDir;
                    ApplyFlip();
                }
            }
        }

        if (overrideY)
        {
            v.y = velocity.y;
        }

        rb.linearVelocity = v;
    }

    public void ForceFacing(int dir)
    {
        if (dir == 0) return;

        int newDir = dir > 0 ? 1 : -1;
        if (newDir == _dir) return;

        _dir = newDir;
        ApplyFlip();
    }

    void Reset()
    {
        if (!rb)       rb       = GetComponent<Rigidbody2D>();
        if (!flipRoot) flipRoot = transform;
    }

    void FixedUpdate()
    {
        if (!rb) return;
        if (IsMovementLocked) return;

        float x = Mathf.Abs(_inputX) > inputDeadzone ? _inputX : 0f;
        float targetVx = x * _s.moveSpeed;

        var v = rb.linearVelocity;
        float currentVx = v.x;
        float dt = Time.fixedDeltaTime;

        // --- 1. Instant turnaround to avoid ice feeling on L/R spam ---
        bool hasInput        = Mathf.Abs(targetVx) > 0.001f;
        bool isMoving        = Mathf.Abs(currentVx) > 0.001f;
        bool directionChanged = hasInput && isMoving &&
                                Mathf.Sign(targetVx) != Mathf.Sign(currentVx);

        if (directionChanged)
        {
            // Snap to full speed in the new direction
            currentVx = targetVx;
        }
        else
        {
            // --- 2. Normal accel/decel when starting/stopping ---
            float accelTime;

            if (hasInput)
            {
                // accelerating toward non-zero target
                accelTime = Mathf.Max(_s.accelerationTime, 0.0001f);
            }
            else
            {
                // decelerating toward zero
                accelTime = Mathf.Max(_s.decelerationTime, 0.0001f);
            }

            float accel    = _s.moveSpeed / accelTime;
            float maxDelta = accel * dt;

            currentVx = Mathf.MoveTowards(currentVx, targetVx, maxDelta);
        }

        v.x = currentVx;
        rb.linearVelocity = v;
    }

    void ApplyFlip()
    {
        if (!flipRoot) return;

        var sc = flipRoot.localScale;
        sc.x = Mathf.Abs(sc.x) * _dir;
        flipRoot.localScale = sc;
    }

    public void StopHorizontal()
    {
        if (!rb) return;

        var v = rb.linearVelocity;
        v.x = 0f;
        rb.linearVelocity = v;
    }
}
