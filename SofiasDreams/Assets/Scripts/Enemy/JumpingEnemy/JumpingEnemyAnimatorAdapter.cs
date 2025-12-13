using UnityEngine;

public class JumpingEnemyAnimatorAdapter : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] Animator _animator;

    [Header("Animator params (names must match controller)")]
    [SerializeField] string _jumpBool = "Jump";
    [SerializeField] string _yVelocityPatrol = "yVelocityPatrol";
    [SerializeField] string _yVelocityAttack = "yVelocityAttack";
    [SerializeField] string _agroTrigger = "Agro";
    [SerializeField] string _patrolTrigger = "Patrol";
    [SerializeField] string _deathFromPatrolTrigger = "DeathFromPatrol";
    [SerializeField] string _deathFromAttackTrigger = "DeathFromAttack";

    [Header("Animator states (names must match controller)")]
    [SerializeField] string _patrolStateName = "Patrol";
    [SerializeField] string _agroTriggerStateName = "AgroTrigger";
    [SerializeField] string _attackStateName = "Attack";
    [SerializeField] string _agroBlendStateName = "Blend Tree Agro";
    [SerializeField] string _patrolBlendStateName = "Blend Tree Patrol";

    int _jumpHash;
    int _yPatrolHash;
    int _yAttackHash;
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
        _yPatrolHash = Animator.StringToHash(_yVelocityPatrol);
        _yAttackHash = Animator.StringToHash(_yVelocityAttack);
        _agroHash = Animator.StringToHash(_agroTrigger);
        _patrolHash = Animator.StringToHash(_patrolTrigger);
        _deathPatrolHash = Animator.StringToHash(_deathFromPatrolTrigger);
        _deathAttackHash = Animator.StringToHash(_deathFromAttackTrigger);
    }

    public void SetJump(bool value)
    {
        if (_animator) _animator.SetBool(_jumpHash, value);
    }

    public void SetPatrolYVelocity(float positiveY)
    {
        if (_animator) _animator.SetFloat(_yPatrolHash, positiveY);
    }

    public void SetAttackYVelocity(float positiveY)
    {
        if (_animator) _animator.SetFloat(_yAttackHash, positiveY);
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

    public bool IsInPatrolLoop()
    {
        if (!_animator) return false;
        var s = _animator.GetCurrentAnimatorStateInfo(0);
        return s.IsName(_patrolStateName) || s.IsName(_patrolBlendStateName);
    }
}

