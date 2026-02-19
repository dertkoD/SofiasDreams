using UnityEngine;

public class JumpingEnemyAnimatorAdapter : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] Animator _animator;

    [Header("Animator params (names must match controller)")]
    [SerializeField] string _yVelocity = "yVelocity";
    [SerializeField] string _landingTrigger = "Landing";
    [SerializeField] string _triggerWindup = "TriggerWindup";
    [SerializeField] string _agroTrigger = "Agro";
    [SerializeField] string _patrolTrigger = "Patrol";
    [SerializeField] string _deathFromPatrolTrigger = "DeathFromPatrol";
    [SerializeField] string _deathFromAttackTrigger = "DeathFromAttack";

    [Header("Animator states (names must match controller)")]
    [SerializeField] string _patrolTriggerStateName = "PatrolTrigger";
    [SerializeField] string _agroTriggerStateName = "AgroTrigger";
    [SerializeField] string _patrolBlendStateName = "PatrolBlendTree";
    [SerializeField] string _agroBlendStateName = "AgroBlendTree";
    [SerializeField] string _patrolLandingStateName = "Patrol Landing";
    [SerializeField] string _agroLandingStateName = "Agro Landing";

    int _yVelocityHash;
    int _landingHash;
    int _triggerWindupHash;
    int _agroHash;
    int _patrolHash;
    int _deathPatrolHash;
    int _deathAttackHash;
    int _patrolBlendHash;
    int _agroBlendHash;
    bool _hasYVelocity;
    bool _hasTriggerWindup;
    bool _landingPaused;

    void Reset()
    {
        _animator = GetComponentInChildren<Animator>(true);
    }

    void Awake()
    {
        if (_animator == null)
            _animator = GetComponentInChildren<Animator>(true);

        _yVelocityHash = FindParam(_yVelocity, "yVelocity", out _hasYVelocity);
        _landingHash = FindParam(_landingTrigger, "Landing", out _);
        _triggerWindupHash = FindParam(_triggerWindup, "TriggerWindup", out _hasTriggerWindup);
        _agroHash = FindParam(_agroTrigger, "Agro", out _);
        _patrolHash = FindParam(_patrolTrigger, "Patrol", out _);
        _deathPatrolHash = FindParam(_deathFromPatrolTrigger, "DeathFromPatrol", out _);
        _deathAttackHash = FindParam(_deathFromAttackTrigger, "DeathFromAttack", out _);
        _patrolBlendHash = Animator.StringToHash(_patrolBlendStateName);
        _agroBlendHash = Animator.StringToHash(_agroBlendStateName);
    }

    int FindParam(string serialized, string fallback, out bool found)
    {
        found = false;
        if (_animator == null) return Animator.StringToHash(fallback);

        foreach (var p in _animator.parameters)
        {
            if (p.name == serialized)
            {
                found = true;
                return p.nameHash;
            }
        }

        foreach (var p in _animator.parameters)
        {
            if (p.name == fallback)
            {
                found = true;
                return p.nameHash;
            }
        }

        return Animator.StringToHash(fallback);
    }

    public void SetYVelocity(float value)
    {
        if (_animator && _hasYVelocity) _animator.SetFloat(_yVelocityHash, value);
    }

    public void RestartBlendTree(bool isAggro)
    {
        if (!_animator) return;
        int hash = isAggro ? _agroBlendHash : _patrolBlendHash;
        var info = _animator.GetCurrentAnimatorStateInfo(0);
        if (info.shortNameHash == hash)
            _animator.Play(hash, 0, 0f);
    }

    public void FireLanding()
    {
        if (_animator) _animator.SetTrigger(_landingHash);
    }

    public void FireTriggerWindup()
    {
        if (_animator && _hasTriggerWindup) _animator.SetTrigger(_triggerWindupHash);
    }

    public bool IsLandingAnimationDone()
    {
        if (!_animator) return true;
        var s = _animator.GetCurrentAnimatorStateInfo(0);
        bool inLanding = s.IsName(_patrolLandingStateName) || s.IsName(_agroLandingStateName);
        if (!inLanding) return true;
        return s.normalizedTime >= 1.0f;
    }

    public void PauseLanding()
    {
        if (!_animator || _landingPaused) return;
        _landingPaused = true;
        _animator.speed = 0f;
    }

    public void ResumeLanding()
    {
        if (!_animator || !_landingPaused) return;
        _landingPaused = false;
        _animator.speed = 1f;
    }

    public void TriggerAgro()
    {
        if (_animator) _animator.SetTrigger(_agroHash);
    }

    public void TriggerPatrol()
    {
        if (_animator) _animator.SetTrigger(_patrolHash);
    }

    public void TriggerDeathFromPatrol()
    {
        if (_animator) _animator.SetTrigger(_deathPatrolHash);
    }

    public void TriggerDeathFromAttack()
    {
        if (_animator) _animator.SetTrigger(_deathAttackHash);
    }

    public bool IsInLanding()
    {
        if (!_animator) return false;
        var s = _animator.GetCurrentAnimatorStateInfo(0);
        return s.IsName(_patrolLandingStateName) || s.IsName(_agroLandingStateName);
    }

    public bool IsInPatrolLanding()
    {
        if (!_animator) return false;
        return _animator.GetCurrentAnimatorStateInfo(0).IsName(_patrolLandingStateName);
    }

    public bool IsInAgroLanding()
    {
        if (!_animator) return false;
        return _animator.GetCurrentAnimatorStateInfo(0).IsName(_agroLandingStateName);
    }

    public bool IsInAttackLoop()
    {
        if (!_animator) return false;
        return _animator.GetCurrentAnimatorStateInfo(0).IsName(_agroBlendStateName);
    }

    public bool IsInAgroTrigger()
    {
        if (!_animator) return false;
        return _animator.GetCurrentAnimatorStateInfo(0).IsName(_agroTriggerStateName);
    }

    public bool IsInPatrolTrigger()
    {
        if (!_animator) return false;
        return _animator.GetCurrentAnimatorStateInfo(0).IsName(_patrolTriggerStateName);
    }

    public bool IsInPatrolLoop()
    {
        if (!_animator) return false;
        return _animator.GetCurrentAnimatorStateInfo(0).IsName(_patrolBlendStateName);
    }
}
