using System;
using System.Collections;
using UnityEngine;
using Zenject;

public class DaggerCombat : MonoBehaviour, IInitializable, IDisposable
{
    [SerializeField] Rigidbody2D rb;

    SignalBus _bus;
    DaggerAttackConfig _cfg;
    DaggerMomentum _momentum;

    int _step;
    bool _attacking;
    bool _queued;
    bool _parrying;
    float _chargedCooldownTimer;
    float _parryCooldownTimer;

    float _origGravity;
    Coroutine _slowFallCo;
    bool _slowFallActive;

    bool _airFreezeUsedThisJump;
    bool _airFreezeActiveNow;
    bool _parryFreezeActive;

    public bool IsAttacking => _attacking;
    public bool IsParrying => _parrying;

    public float CurrentDamage =>
        _step == 1 ? (_cfg ? _cfg.damage1 : 8f) :
        _step == 2 ? (_cfg ? _cfg.damage2 : 8f) :
        (_cfg ? _cfg.superDamage : 25f);

    [Inject]
    void Construct(SignalBus bus,
        [Inject(Optional = true)] DaggerAttackConfig cfg = null,
        [Inject(Optional = true)] DaggerMomentum momentum = null)
    {
        _bus = bus;
        _cfg = cfg;
        _momentum = momentum;
    }

    public void Configure(DaggerAttackConfig cfg) => _cfg = cfg;

    public void Initialize()
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        _origGravity = rb ? rb.gravityScale : 1f;
        _bus.Subscribe<AttackStarted>(OnAirAttackStarted);
        _bus.Subscribe<AttackFinished>(OnAirAttackFinished);
        _bus.Subscribe<GroundedChanged>(OnGroundedChanged);
    }

    public void Dispose()
    {
        _bus.TryUnsubscribe<AttackStarted>(OnAirAttackStarted);
        _bus.TryUnsubscribe<AttackFinished>(OnAirAttackFinished);
        _bus.TryUnsubscribe<GroundedChanged>(OnGroundedChanged);
    }

    // ───── Combo (2 hits) ─────

    public void RequestAttack()
    {
        if (_attacking) { if (_step < 2) _queued = true; return; }

        _attacking = true;
        _queued = false;
        _step = (_step % 2) + 1;
        _bus.Fire(new AttackStarted { mode = AttackMode.DaggerCombo, index = _step });
    }

    public void DaggerFinishFromAnimation()
    {
        if (!_attacking) return;

        if (_queued && _step < 2)
        {
            _queued = false;
            _attacking = false;
            _step++;
            _attacking = true;
            _bus.Fire(new AttackStarted { mode = AttackMode.DaggerCombo, index = _step });
            return;
        }

        var mode = _step == 3 ? AttackMode.DaggerSuper : AttackMode.DaggerCombo;
        _attacking = false;
        _bus.Fire(new AttackFinished { mode = mode, index = _step });
        _step = 0;
    }

    // ───── Charged attack (independent) ─────

    public bool IsChargedReady => _chargedCooldownTimer <= 0f;
    public bool IsParryReady => _parryCooldownTimer <= 0f;

    void Update()
    {
        if (_chargedCooldownTimer > 0f)
            _chargedCooldownTimer -= Time.deltaTime;
        if (_parryCooldownTimer > 0f)
            _parryCooldownTimer -= Time.deltaTime;
    }

    public void RequestChargedAttack()
    {
        if (_chargedCooldownTimer > 0f) return;
        if (_attacking) Interrupt();

        _attacking = true;
        _step = 3;
        _chargedCooldownTimer = _cfg ? _cfg.chargedCooldown : 1.5f;
        StopSlowFall();
        _bus.Fire(new AttackStarted { mode = AttackMode.DaggerSuper, index = 3 });
        Debug.Log($"[DaggerCombat] ChargedAttack fired! force={(_cfg ? _cfg.playerLaunchForce : 0)}");
    }

    public void LaunchCharged()
    {
        if (!_attacking || _step != 3) return;
        _slowFallCo = StartCoroutine(ChargedLaunchRoutine());
        Debug.Log("[DaggerCombat] ChargedAttack — animation done, launching player");
    }

    public void Interrupt()
    {
        if (!_attacking && _step == 0) return;

        StopSlowFall();

        var mode = _step == 3 ? AttackMode.DaggerSuper : AttackMode.DaggerCombo;
        _attacking = false;
        _queued = false;
        _step = 0;
        _bus.Fire(new AttackFinished { mode = mode, index = 0 });
    }

    // ───── Charged attack launch + slow-fall ─────

    IEnumerator ChargedLaunchRoutine()
    {
        if (!rb || _cfg == null)
        {
            DaggerFinishFromAnimation();
            yield break;
        }

        yield return new WaitForFixedUpdate();

        float force = _cfg.playerLaunchForce;
        rb.linearVelocity = new Vector2(0f, 0f);
        rb.AddForce(Vector2.up * (force * rb.mass), ForceMode2D.Impulse);

        Debug.Log($"[DaggerCombat] Launch applied! vel={rb.linearVelocity}");

        while (rb && rb.linearVelocity.y > 0f)
            yield return null;

        if (!rb)
        {
            DaggerFinishFromAnimation();
            yield break;
        }

        rb.gravityScale = _cfg.floatGravityScale;
        _slowFallActive = true;
        Debug.Log($"[DaggerCombat] Slow-fall ON, gravityScale={rb.gravityScale}");

        yield return new WaitForSeconds(_cfg.floatGravityDuration);

        EndSlowFall();
        _slowFallCo = null;
        DaggerFinishFromAnimation();
    }

    void StopSlowFall()
    {
        if (_slowFallCo != null)
        {
            StopCoroutine(_slowFallCo);
            _slowFallCo = null;
        }
        EndSlowFall();
    }

    void EndSlowFall()
    {
        if (!_slowFallActive) return;
        _slowFallActive = false;
        if (rb) rb.gravityScale = _origGravity;
        Debug.Log($"[DaggerCombat] Slow-fall OFF, gravityScale={_origGravity}");
    }

    // ───── Parry ─────

    public void RequestParry()
    {
        _parrying = true;
        _parryCooldownTimer = _cfg ? _cfg.parryCooldown : 1f;

        if (rb && rb.linearVelocity.y != 0f)
        {
            StopSlowFall();
            _parryFreezeActive = true;
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;
            Debug.Log("[DaggerCombat] Parry air-freeze ON");
        }
    }

    public void ParryFinishFromAnimation()
    {
        _parrying = false;
        EndParryFreeze();
        _bus.Fire(new ParryFinished());
    }

    public bool TryExecuteParry(Transform attacker)
    {
        if (attacker == null) return false;

        _parrying = false;
        EndParryFreeze();

        TeleportToOtherSide(attacker);
        StunEnemy(attacker);
        _momentum?.OnParrySuccess();
        _bus.Fire(new ParryFinished());

        Debug.Log("[DaggerCombat] Parry successful!");
        return true;
    }

    void EndParryFreeze()
    {
        if (!_parryFreezeActive) return;
        _parryFreezeActive = false;
        if (rb) rb.gravityScale = _origGravity;
        Debug.Log($"[DaggerCombat] Parry air-freeze OFF, gravityScale={_origGravity}");
    }

    void TeleportToOtherSide(Transform enemy)
    {
        if (!rb) return;

        float diff = rb.position.x - enemy.position.x;
        var mover = GetComponent<Mover2D>();

        float playerSide;
        if (Mathf.Abs(diff) > 0.01f)
            playerSide = Mathf.Sign(diff);
        else
            playerSide = mover ? mover.FacingDir : 1f;

        float offset = _cfg != null ? _cfg.parryTeleportOffset : 1.5f;
        float targetX = enemy.position.x - playerSide * offset;

        rb.position = new Vector2(targetX, rb.position.y);

        int faceTowardEnemy = (int)Mathf.Sign(enemy.position.x - targetX);
        if (mover) mover.ForceFacing(faceTowardEnemy);
    }

    void StunEnemy(Transform enemy)
    {
        var kb = enemy.GetComponentInChildren<Knockback2D>();
        if (kb == null) return;

        float stunDur = _cfg != null ? _cfg.parryStunDuration : 1f;
        var info = new DamageInfo
        {
            amount = 0,
            impulse = Vector2.zero,
            stunSeconds = stunDur
        };
        kb.Apply(info);
    }

    // ───── Air attack freeze (once per jump) ─────

    static bool IsDaggerAirMode(AttackMode m) =>
        m is AttackMode.DaggerFlyUp or AttackMode.DaggerFlyDown;

    void OnAirAttackStarted(AttackStarted s)
    {
        if (!IsDaggerAirMode(s.mode) || !rb) return;

        if (_airFreezeUsedThisJump) return;

        _airFreezeUsedThisJump = true;
        _airFreezeActiveNow = true;

        StopSlowFall();

        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;

        Debug.Log("[DaggerCombat] Air-freeze ON (first air attack this jump)");
    }

    void OnAirAttackFinished(AttackFinished s)
    {
        if (!IsDaggerAirMode(s.mode)) return;

        if (_airFreezeActiveNow)
        {
            _airFreezeActiveNow = false;
            rb.gravityScale = _origGravity;
            Debug.Log($"[DaggerCombat] Air-freeze OFF, gravityScale={_origGravity}");
        }
    }

    void OnGroundedChanged(GroundedChanged g)
    {
        if (!g.grounded) return;

        _airFreezeUsedThisJump = false;
        _airFreezeActiveNow = false;
        _parryFreezeActive = false;

        if (_slowFallCo != null)
        {
            StopCoroutine(_slowFallCo);
            _slowFallCo = null;
        }

        if (_slowFallActive)
        {
            _slowFallActive = false;
            Debug.Log("[DaggerCombat] Landed — slow-fall cancelled");
        }

        if (rb) rb.gravityScale = _origGravity;
    }
}
