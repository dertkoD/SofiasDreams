using System;
using UnityEngine;

public class PlayerRangedAbility : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private KeyCode shootKey = KeyCode.Mouse1;

    [Header("Flip & Spawn")]
    [SerializeField] private Transform flipRoot;
    [SerializeField] private Transform muzzle;

    [Header("Prefabs")]
    [SerializeField] private GameObject muzzleFlashPrefab;
    [SerializeField] private PowershotProjectile projectilePrefab;

    [Header("Player Physics")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private PlayerJump jump;

    [Header("Air Freeze")]
    [SerializeField] private bool freezeYWhenAirborne = true;
    [SerializeField] private bool zeroGravityWhileFrozen = true;

    [Header("Cooldown")]
    [SerializeField] private float cooldownSeconds = 10f;
    public bool IsOnCooldown => _cooldownRemaining > 0f;
    public float CooldownRemaining => Mathf.Max(0f, _cooldownRemaining);
    public float CooldownDuration => cooldownSeconds;
    public float Cooldown01 => (cooldownSeconds <= 0f) ? 0f : Mathf.Clamp01(_cooldownRemaining / cooldownSeconds);

    public event Action<float, float> OnCooldownTick; // (remaining, duration)
    public event Action<bool> OnReadyChanged;         // true when becomes ready

    public bool IsShotLocked { get; private set; }

    bool CanShoot => !IsShotLocked && !IsOnCooldown;

    private Animator _anim;
    private RigidbodyConstraints2D _preConstraints;
    private float _preGravity;
    private float _cooldownRemaining;
    private bool _wasReady = true;

    void Awake()
    {
        if (!_anim)       _anim    = GetComponent<Animator>();
        if (!rb)          rb       = GetComponent<Rigidbody2D>();
        if (!flipRoot)    flipRoot = transform;
        if (!movement)    movement = GetComponentInParent<PlayerMovement>();
        if (!jump)        jump     = GetComponentInParent<PlayerJump>();
    }

    void Update()
    {
        // Tick cooldown first, every frame.
        if (_cooldownRemaining > 0f)
        {
            _cooldownRemaining -= Time.deltaTime;
            if (_cooldownRemaining < 0f) _cooldownRemaining = 0f;
            OnCooldownTick?.Invoke(_cooldownRemaining, cooldownSeconds);
        }

        // Fire ready-change edge event
        bool isReady = !IsOnCooldown;
        if (isReady != _wasReady)
        {
            _wasReady = isReady;
            OnReadyChanged?.Invoke(isReady);
        }

        // While attack animation is locking, we exit early.
        if (IsShotLocked) return;

        // Single source of truth: CanShoot must be true.
        if (Input.GetKeyDown(shootKey) && CanShoot)
        {
            StartShot();
        }
    }

    private void StartShot()
    {
        // Double guard (covers any external calls or race conditions).
        if (!CanShoot) return;

        BeginCooldown();                 // start cooldown IMMEDIATELY on commit
        _anim.SetTrigger("Powershot");
        SetLocked(true);
    }

    private void BeginCooldown()
    {
        _cooldownRemaining = cooldownSeconds;
        OnCooldownTick?.Invoke(_cooldownRemaining, cooldownSeconds);
        if (_wasReady) { _wasReady = false; OnReadyChanged?.Invoke(false); }
    }

    private void SetLocked(bool locked)
    {
        if (IsShotLocked == locked) return;
        IsShotLocked = locked;

        if (IsShotLocked)
        {
            if (rb)
            {
                _preConstraints = rb.constraints;
                _preGravity     = rb.gravityScale;

                var constraints = _preConstraints
                                  | RigidbodyConstraints2D.FreezePositionX
                                  | RigidbodyConstraints2D.FreezePositionY
                                  | RigidbodyConstraints2D.FreezeRotation;

                rb.constraints = constraints;

                if (zeroGravityWhileFrozen) rb.gravityScale = 0f;
            }
            if (movement) movement.SetMovementLocked(true);
        }
        else
        {
            if (rb)
            {
                rb.constraints  = _preConstraints;
                rb.gravityScale = _preGravity;
            }
            if (movement) movement.SetMovementLocked(false);
        }
    }

    // === Animation Events ===
    public void AnimEvent_SpawnMuzzleFlash()
    {
        if (!muzzle || !muzzleFlashPrefab) return; 
        Instantiate(muzzleFlashPrefab, muzzle.position, muzzle.rotation, muzzle);
    }

    public void AnimEvent_SpawnProjectile()
    {
        if (!muzzle || !projectilePrefab) return;

        bool facingRight = (flipRoot ? flipRoot.lossyScale.x : transform.lossyScale.x) >= 0f;
        var proj = Instantiate(projectilePrefab, muzzle.position, Quaternion.identity);
        proj.Initialize(owner: transform, direction: facingRight ? Vector2.right : Vector2.left);

        // Safety: if someone removed BeginCooldown from StartShot, this keeps it from being spammable.
        if (!IsOnCooldown) BeginCooldown();
    }

    public void AnimEvent_ShootEnd()
    {
        SetLocked(false);
        if (rb)
        {
            var v = rb.linearVelocity;
            if (v.y > 0f) v.y = -0.1f;
            rb.linearVelocity = v;
        }
    }
}
