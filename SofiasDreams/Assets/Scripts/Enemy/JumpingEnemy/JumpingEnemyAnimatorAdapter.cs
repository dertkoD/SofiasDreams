using UnityEngine;

public class JumpingEnemyAnimatorAdapter : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] Animator _animator;

    [Header("Animator params (names must match controller)")]
    [SerializeField] string _jumpBool = "Jump";
    [SerializeField] string _triggerPoint = "TriggerPoint";
    [SerializeField] string _triggerLanding = "TriggerLanding";
    [SerializeField] string _agroTrigger = "Agro";
    [SerializeField] string _patrolTrigger = "Patrol";
    [SerializeField] string _deathFromPatrolTrigger = "DeathFromPatrol";
    [SerializeField] string _deathFromAttackTrigger = "DeathFromAttack";

    [Header("Animator states (names must match controller)")]
    [SerializeField] string _patrolStateName = "Patrol";
    [SerializeField] string _patrolTriggerStateName = "PatrolTrigger";
    [SerializeField] string _agroTriggerStateName = "AgroTrigger";
    [SerializeField] string _attackStateName = "Attack";
    [SerializeField] string _patrolWindupStateName = "PatrolWindup";
    [SerializeField] string _hightPointPatrolStateName = "HightPointPatrol";
    [SerializeField] string _patrolLandingStateName = "PatrolLanding";
    [SerializeField] string _attackWindupStateName = "AttackWindup";
    [SerializeField] string _hightPointAttackStateName = "HightPointAttack";
    [SerializeField] string _attackLandingStateName = "AttackLanding";

    int _jumpHash;
    int _triggerPointHash;
    int _triggerLandingHash;
    int _agroHash;
    int _patrolHash;
    int _deathPatrolHash;
    int _deathAttackHash;

    void Reset()
    {
        _animator = GetComponentInChildren<Animator>(true);
    }

    void Awake()
    {
        if (_animator == null)
            _animator = GetComponentInChildren<Animator>(true);

        _jumpHash = Animator.StringToHash(_jumpBool);
        _triggerPointHash = Animator.StringToHash(_triggerPoint);
        _triggerLandingHash = Animator.StringToHash(_triggerLanding);
        _agroHash = Animator.StringToHash(_agroTrigger);
        _patrolHash = Animator.StringToHash(_patrolTrigger);
        _deathPatrolHash = Animator.StringToHash(_deathFromPatrolTrigger);
        _deathAttackHash = Animator.StringToHash(_deathFromAttackTrigger);
    }

    public void SetJump(bool value)
    {
        if (_animator) _animator.SetBool(_jumpHash, value);
    }

    public void FireTriggerPoint()
    {
        if (_animator) _animator.SetTrigger(_triggerPointHash);
    }

    public void FireTriggerLanding()
    {
        if (_animator) _animator.SetTrigger(_triggerLandingHash);
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
        return s.IsName(_attackStateName)
            || s.IsName(_attackWindupStateName)
            || s.IsName(_hightPointAttackStateName)
            || s.IsName(_attackLandingStateName);
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
        return s.IsName(_patrolStateName)
            || s.IsName(_patrolWindupStateName)
            || s.IsName(_hightPointPatrolStateName)
            || s.IsName(_patrolLandingStateName);
    }
}
