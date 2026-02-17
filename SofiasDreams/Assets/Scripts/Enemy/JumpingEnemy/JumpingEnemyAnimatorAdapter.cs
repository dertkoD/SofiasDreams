using UnityEngine;

public class JumpingEnemyAnimatorAdapter : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] Animator _animator;

    [Header("Animator params (names must match controller)")]
    [SerializeField] string _yVelocity = "yVelocity";
    [SerializeField] string _landingTrigger = "Landing";
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
    int _agroHash;
    int _patrolHash;
    int _deathPatrolHash;
    int _deathAttackHash;
    int _patrolBlendHash;
    int _agroBlendHash;

    void Reset()
    {
        _animator = GetComponentInChildren<Animator>(true);
    }

    void Awake()
    {
        if (_animator == null)
            _animator = GetComponentInChildren<Animator>(true);

        _yVelocityHash = ResolveParamHash(_yVelocity, "yVelocity");
        _landingHash = ResolveParamHash(_landingTrigger, "Landing");
        _agroHash = ResolveParamHash(_agroTrigger, "Agro");
        _patrolHash = ResolveParamHash(_patrolTrigger, "Patrol");
        _deathPatrolHash = ResolveParamHash(_deathFromPatrolTrigger, "DeathFromPatrol");
        _deathAttackHash = ResolveParamHash(_deathFromAttackTrigger, "DeathFromAttack");
        _patrolBlendHash = Animator.StringToHash(_patrolBlendStateName);
        _agroBlendHash = Animator.StringToHash(_agroBlendStateName);
    }

    int ResolveParamHash(string serialized, string fallback)
    {
        if (!string.IsNullOrEmpty(serialized))
        {
            int hash = Animator.StringToHash(serialized);
            if (_animator != null && HasParam(hash)) return hash;
        }
        return Animator.StringToHash(fallback);
    }

    bool HasParam(int hash)
    {
        foreach (var p in _animator.parameters)
            if (p.nameHash == hash) return true;
        return false;
    }

    public void SetYVelocity(float value)
    {
        if (_animator) _animator.SetFloat(_yVelocityHash, value);
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
