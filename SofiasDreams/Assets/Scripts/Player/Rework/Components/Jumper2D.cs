using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class Jumper2D : MonoBehaviour, IJumper
{
    [Header("Ground check")]
    [SerializeField] Transform groundCheck;
    [SerializeField] float groundRadius = 0.1f;
    [SerializeField] LayerMask groundMask = ~0;
    [SerializeField] Rigidbody2D rb;
    
    [Header("Drop-through")]
    [SerializeField] Collider2D playerCollider;

    IMobilityGate _gate;
    JumpSettings _s;
    SignalBus _bus;

    float _coyote;
    float _buffer;
    bool _isJumping;
    bool _isDropping;
    bool _wasGrounded;
    bool _wantsJumpCut;
    public bool IsGrounded { get; private set; }
    
    // Buffers for contacts during drop-through
    readonly List<Collider2D> _contactBuffer    = new(8);
    readonly List<Collider2D> _ignoredThisDrop  = new(8);

    public void Configure(JumpSettings s) => _s = s;
    [Inject] public void Inject(IMobilityGate gate, SignalBus bus) { _gate = gate; _bus = bus; }

    public void RequestJump() => _buffer = _s.jumpBufferTime;

    public void RequestDropThrough()
    {
        if (_isDropping) return;
        if (!IsGrounded) return;
        if (!rb)         return;

        StartCoroutine(DropRoutine());
    }

    void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        if (!playerCollider)
            playerCollider = GetComponent<Collider2D>();
    }

    void OnEnable()
    {
        _wasGrounded = IsGrounded = CheckGrounded();             
        _bus?.Fire(new GroundedChanged { grounded = IsGrounded });
    }

    void Update()
    {
        IsGrounded = CheckGrounded();

        if (IsGrounded != _wasGrounded)
        {
            _bus?.Fire(new GroundedChanged { grounded = IsGrounded });
            _wasGrounded = IsGrounded;
        }

        _coyote = IsGrounded ? _s.coyoteTime : Mathf.Max(0f, _coyote - Time.deltaTime);
        _buffer = Mathf.Max(0f, _buffer - Time.deltaTime);

        if (IsGrounded && _isJumping) _isJumping = false;
    }

    void FixedUpdate()
    {
        if (!rb) return;

        if (_gate != null && _gate.IsJumpBlocked)
        {
            _buffer = 0f;
            return;
        }

        // existing buffered jump logic
        if (_buffer > 0f && (_coyote > 0f || IsGrounded))
        {
            var v = rb.linearVelocity;
            v.y = _s.jumpVelocity;
            rb.linearVelocity = v;

            _isJumping = true;
            IsGrounded = false;
            _buffer = 0f;
            _coyote = 0f;
        }

        // Variable-height jump cut
        if (_wantsJumpCut)
        {
            _wantsJumpCut = false;

            // Only cut if we're moving up and not currently grounded
            if (!IsGrounded && rb.linearVelocity.y > 0f)
            {
                var v = rb.linearVelocity;
                v.y *= _s.jumpCutMultiplier;
                rb.linearVelocity = v;
            }
        }
    }
    
    bool CheckGrounded()
    {
        var filter = new ContactFilter2D
        {
            useTriggers    = false,
            useLayerMask   = true,
            layerMask      = groundMask,
            useNormalAngle = true,
            minNormalAngle = 80f,
            maxNormalAngle = 100f
        };

        return rb.IsTouching(filter);
    }
    
    void BroadcastGroundedImmediate(bool grounded)
    {
        if (_wasGrounded == grounded)
            return;

        _wasGrounded = grounded;
        _bus?.Fire(new GroundedChanged { grounded = grounded });
    }

    IEnumerator DropRoutine()
    {
        _isDropping = true;

        // Stop upward motion so we separate cleanly
        if (rb.linearVelocity.y > 0f)
        {
            var v = rb.linearVelocity;
            v.y = 0f;
            rb.linearVelocity = v;
        }

        // Collect current contacts touching the player (no layers needed)
        _contactBuffer.Clear();
        _ignoredThisDrop.Clear();

        var filter = new ContactFilter2D
        {
            useTriggers    = false,
            useLayerMask   = false,
            useNormalAngle = false,
        };

        if (!playerCollider)
            playerCollider = GetComponent<Collider2D>();

        if (playerCollider != null && playerCollider.attachedRigidbody != null)
        {
            playerCollider.attachedRigidbody.GetContacts(filter, _contactBuffer);

            foreach (var col in _contactBuffer)
            {
                if (col == null) continue;

                // Accept effector on the collider or any parent
                var eff = col.GetComponent<PlatformEffector2D>() ?? col.GetComponentInParent<PlatformEffector2D>();
                if (eff == null) continue;

                Physics2D.IgnoreCollision(playerCollider, col, true);
                _ignoredThisDrop.Add(col);
            }
        }

        // Gentle nudge down helps break contact immediately
        rb.AddForce(Vector2.down * 2.5f, ForceMode2D.Impulse);

        // We consider ourselves no longer grounded for logic (coyote starts ticking)
        IsGrounded = false;
        BroadcastGroundedImmediate(false);

        yield return new WaitForSeconds(_s.dropDuration);

        // Re-enable all ignored collisions
        foreach (var col in _ignoredThisDrop)
        {
            if (col == null) continue;
            Physics2D.IgnoreCollision(playerCollider, col, false);
        }
        _ignoredThisDrop.Clear();

        _isDropping = false;
    }
    
    public void NotifyJumpReleased()
    {
        _wantsJumpCut = true;
    }
}
