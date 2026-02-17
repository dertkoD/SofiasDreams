using UnityEngine;

public class JumpingEnemyAnimatorAdapter : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] Animator _animator;

    [Header("Animator params (names must match controller)")]
    [SerializeField] string _jumpBool = "Jump";
    [SerializeField] string _yVelocity = "yVelocity";
    [SerializeField] string _agroTrigger = "Agro";
    [SerializeField] string _patrolTrigger = "Patrol";
    [SerializeField] string _deathFromPatrolTrigger = "DeathFromPatrol";
    [SerializeField] string _deathFromAttackTrigger = "DeathFromAttack";

    [Header("Animator states (names must match controller)")]
    [SerializeField] string _patrolStateName = "Patrol";
    [SerializeField] string _patrolTriggerStateName = "PatrolTrigger";
    [SerializeField] string _agroTriggerStateName = "AgroTrigger";
    [SerializeField] string _attackStateName = "Attack";
    [SerializeField] string _patrolBlendStateName = "PatrolBlendTree";
    [SerializeField] string _agroBlendStateName = "AgroBlendTree";

    int _jumpHash;
    int _yVelocityHash;
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

        _jumpHash = Animator.StringToHash(_jumpBool);
        _yVelocityHash = Animator.StringToHash(_yVelocity);
        _agroHash = Animator.StringToHash(_agroTrigger);
        _patrolHash = Animator.StringToHash(_patrolTrigger);
        _deathPatrolHash = Animator.StringToHash(_deathFromPatrolTrigger);
        _deathAttackHash = Animator.StringToHash(_deathFromAttackTrigger);
        _patrolBlendHash = Animator.StringToHash(_patrolBlendStateName);
        _agroBlendHash = Animator.StringToHash(_agroBlendStateName);
    }

    public void SetJump(bool value)
    {
        if (_animator) _animator.SetBool(_jumpHash, value);
    }

    public void SetYVelocity(float value)
    {
        if (_animator) _animator.SetFloat(_yVelocityHash, value);
    }

    /// <summary>
    /// Restarts the active blend tree state from normalizedTime 0 so the
    /// landing clip plays from its first frame when the jump phase changes.
    /// </summary>
    public void RestartBlendTree(bool isAggro)
    {
        if (!_animator) return;
        int hash = isAggro ? _agroBlendHash : _patrolBlendHash;
        var info = _animator.GetCurrentAnimatorStateInfo(0);
        if (info.shortNameHash == hash)
            _animator.Play(hash, 0, 0f);
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

    public bool IsInAttackLoop()
    {
        if (!_animator) return false;
        var s = _animator.GetCurrentAnimatorStateInfo(0);
        return s.IsName(_attackStateName) || s.IsName(_agroBlendStateName);
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
        var s = _animator.GetCurrentAnimatorStateInfo(0);
        return s.IsName(_patrolStateName) || s.IsName(_patrolBlendStateName);
    }
}
