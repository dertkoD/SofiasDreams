using System.Collections;
using UnityEngine;
using Zenject;

public class Dasher2D : MonoBehaviour, IDasher
{
    [Header("Physics")]
    [SerializeField] Rigidbody2D rb;

    [Header("Ghosts")]
    [SerializeField] SpriteRenderer[] ghosts;      
    [SerializeField] float minGhostDistance = 0.05f; 

    DashSettings  _s;
    IMobilityGate _gate;
    SignalBus     _bus;

    bool   _dashing;
    float  _cd;
    float  _savedGravity;
    bool   _inAirOnStart;
    float  _dir;
    Coroutine _dashCo;

    Vector2 _lastGhostPos;
    bool    _hasLastGhostPos;

    public bool IsDashing => _dashing;
    DashPhase _phase = DashPhase.None;
    float _currentSpeed;

    [Inject]
    public void Inject(IMobilityGate gate, SignalBus bus)
    {
        _gate = gate;
        _bus  = bus;
    }

    public void Configure(DashSettings settings)
    {
        _s = settings;
    }

    void Reset()
    {
        if (!rb)
            rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (_cd > 0f)
            _cd -= Time.deltaTime;
    }
    
    public bool RequestDash(float direction, bool isGrounded)
    {
        if (_dashing) return false;
        if (_cd > 0f) return false;
        if (!_s.allowAirDash && !isGrounded) return false;
        if (!rb) return false;

        if (Mathf.Abs(direction) < 0.01f)
            direction = transform.localScale.x >= 0 ? 1f : -1f;

        _dir          = Mathf.Sign(direction);
        _dashing      = true;
        _cd           = _s.cooldown;
        _inAirOnStart = !isGrounded;

        _gate.BlockMovement(MobilityBlockReason.Dash);
        _gate.BlockJump(MobilityBlockReason.Dash);

        _savedGravity   = rb.gravityScale;
        rb.gravityScale = 0f;

        var v = rb.linearVelocity;
        v.y = 0f;
        rb.linearVelocity = v;

        _bus.Fire(new DashStarted { inAir = _inAirOnStart });

        _lastGhostPos    = rb.position;
        _hasLastGhostPos = true;
        
        _currentSpeed = 0f;
        _phase        = DashPhase.Accel;

        if (_dashCo != null)
            StopCoroutine(_dashCo);
        _dashCo = StartCoroutine(DashRoutine());

        return true;
    }

    IEnumerator DashRoutine()
    {
        while (_dashing)
        {
            float targetSpeed = 0f;
            float a           = 0f;

            switch (_phase)
            {
                case DashPhase.Accel:
                    targetSpeed = _s.dashSpeed;
                    a           = _s.accel;
                    break;

                case DashPhase.Constant:
                    targetSpeed = _s.dashSpeed;
                    a           = 0f;
                    break;

                case DashPhase.Decel:
                    targetSpeed = 0f;
                    a           = _s.decel;
                    break;

                default:
                    targetSpeed = 0f;
                    a           = 0f;
                    break;
            }

            if (a > 0f)
                _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, a * Time.deltaTime);
            else
                _currentSpeed = targetSpeed;

            var vel = rb.linearVelocity;
            vel.x = _dir * _currentSpeed;
            rb.linearVelocity = vel;

            yield return null;
        }
    }
    
    public void FinishFromAnimationDash()
    {
        if (!_dashing)
            return;

        _dashing = false;

        if (rb)
            rb.gravityScale = _savedGravity;

        _gate.UnblockMovement(MobilityBlockReason.Dash);
        _gate.UnblockJump(MobilityBlockReason.Dash);

        _bus.Fire(new DashFinished { inAir = _inAirOnStart });

        if (_dashCo != null)
        {
            StopCoroutine(_dashCo);
            _dashCo = null;
        }

        _hasLastGhostPos = false;
    }
    
    public void DashGhostEvent(int index)
    {
        if (index < 0 || index >= ghosts.Length) return;
        var sr = ghosts[index];
        if (sr == null) return;
        if (!_dashing || rb == null) return;

        if (!_hasLastGhostPos)
        {
            _lastGhostPos    = rb.position;
            _hasLastGhostPos = true;
            return;
        }

        float dx = Mathf.Abs(rb.position.x - _lastGhostPos.x);
        _lastGhostPos = rb.position;

        bool blocked = dx < minGhostDistance;

        if (blocked)
        {
            sr.enabled = false;
        }
        else
        {
            sr.enabled = true;
        }
    }
    
    public void DashPhase_Accel()
    {
        _phase = DashPhase.Accel;
    }

    public void DashPhase_Constant()
    {
        _phase = DashPhase.Constant;
    }

    public void DashPhase_Decel()
    {
        _phase = DashPhase.Decel;
    }
}
