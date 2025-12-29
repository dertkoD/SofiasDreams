using UnityEngine;
using Zenject;

public class Mover2D : MonoBehaviour, IMover
{
    [Header("Refs")]
    [SerializeField] Rigidbody2D rb;
    [SerializeField] Transform  flipRoot;

    [Header("Feel")]
    [SerializeField] float inputDeadzone = 0.05f;

    [Header("VFX - Run Dust (Non-looping)")]
    [SerializeField] ParticleSystem runDust;
    [SerializeField] float dustMinSpeed   = 0.8f;   // speed (abs vx) to start dust
    [SerializeField] float dustInterval   = 0.08f;  // seconds between puffs
    [SerializeField] int   dustBurstCount = 6;      // particles per puff
    [SerializeField] bool  dustRequireGrounded = true;
    
    [Header("Refs")]
    [SerializeField] Jumper2D jumper;
    [SerializeField] bool dustRequireInput = true;
    [SerializeField] bool dustClearOnStop = false;

    IMobilityGate _gate;
    MoveSettings  _s;

    float _inputX;
    bool  _localLocked;
    int   _dir = 1;

    float _dustTimer;

    // If you have a grounded check elsewhere, set this from that script.
    // If you don't set it, it stays true and dust works based on speed only.
    public bool IsGrounded { get; set; } = true;

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

        if (v) ResetDustTimer();
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
            v.y = velocity.y;

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

        float dt = Time.fixedDeltaTime;

        // If movement is locked, do not emit new dust
        if (IsMovementLocked)
        {
            ResetDustTimer();
            StopDust();
            return;
        }

        float x = Mathf.Abs(_inputX) > inputDeadzone ? _inputX : 0f;
        float targetVx = x * _s.moveSpeed;

        var v = rb.linearVelocity;
        float currentVx = v.x;

        // --- 1. Instant turnaround ---
        bool hasInput         = Mathf.Abs(targetVx) > 0.001f;
        bool isMoving         = Mathf.Abs(currentVx) > 0.001f;
        bool directionChanged = hasInput && isMoving && Mathf.Sign(targetVx) != Mathf.Sign(currentVx);

        if (directionChanged)
        {
            currentVx = targetVx;
        }
        else
        {
            // --- 2. Normal accel/decel ---
            float accelTime = hasInput
                ? Mathf.Max(_s.accelerationTime, 0.0001f)
                : Mathf.Max(_s.decelerationTime, 0.0001f);

            float accel    = _s.moveSpeed / accelTime;
            float maxDelta = accel * dt;

            currentVx = Mathf.MoveTowards(currentVx, targetVx, maxDelta);
        }

        v.x = currentVx;
        rb.linearVelocity = v;

        UpdateRunDust(currentVx, dt, x);
    }

    void UpdateRunDust(float vx, float dt, float inputX)
    {
        if (!runDust) return;

        bool grounded = jumper != null && jumper.IsGrounded; 
        if (!grounded)
        {
            ResetDustTimer();
            StopDust();
            return;
        }

        if (dustRequireInput && Mathf.Abs(inputX) <= 0.01f)
        {
            ResetDustTimer();
            StopDust();
            return;
        }

        float speed = Mathf.Abs(vx);
        if (speed < dustMinSpeed)
        {
            ResetDustTimer();
            StopDust();
            return;
        }

        _dustTimer += dt;
        if (_dustTimer < dustInterval)
            return;

        _dustTimer = 0f;

        if (!runDust.isPlaying)
            runDust.Play();

        runDust.Emit(dustBurstCount);
    }

    void ResetDustTimer() => _dustTimer = 0f;

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

        ResetDustTimer();
    }
    
    void StopDust()
    {
        if (!runDust) return;
        if (!runDust.isPlaying) return;

        runDust.Stop(true,
            dustClearOnStop
                ? ParticleSystemStopBehavior.StopEmittingAndClear
                : ParticleSystemStopBehavior.StopEmitting);
    }
}
