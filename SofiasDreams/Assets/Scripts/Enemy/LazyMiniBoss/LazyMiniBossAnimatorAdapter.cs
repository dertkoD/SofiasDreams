using UnityEngine;

public class LazyMiniBossAnimatorAdapter : MonoBehaviour
{
    [SerializeField] Animator _animator;

    [Header("Parameters")]
    [SerializeField] string _xVelocity = "xVelocity";
    [SerializeField] string _triggerAgro = "TriggerAgro";
    [SerializeField] string _triggerPatrol = "TriggerPatrol";
    [SerializeField] string _deathTrigger = "Death";
    [SerializeField] string _attack1Bool = "Attack1";
    [SerializeField] string _attack2Bool = "Attack2";
    [SerializeField] string _attack3Bool = "Attack3";
    [SerializeField] string _shootTrigger = "Shoot";

    [Header("State Names")]
    [SerializeField] string _sleepState = "Sleep";
    [SerializeField] string _agroMovementState = "AgroMovement";
    [SerializeField] string _triggerAgroState = "TriggerAgro";
    [SerializeField] string _triggerPatrolState = "TriggerPatrol";
    [SerializeField] string _attack1State = "Attack1";
    [SerializeField] string _attack2State = "Attack2";
    [SerializeField] string _attack3State = "Attack3";
    [SerializeField] string _shootState = "Shoot";

    int _xVelocityHash;
    int _triggerAgroHash;
    int _triggerPatrolHash;
    int _deathHash;
    int _attack1Hash;
    int _attack2Hash;
    int _attack3Hash;
    int _shootHash;

    void Awake()
    {
        if (!_animator) _animator = GetComponentInChildren<Animator>();

        _xVelocityHash = Animator.StringToHash(_xVelocity);
        _triggerAgroHash = Animator.StringToHash(_triggerAgro);
        _triggerPatrolHash = Animator.StringToHash(_triggerPatrol);
        _deathHash = Animator.StringToHash(_deathTrigger);
        _attack1Hash = Animator.StringToHash(_attack1Bool);
        _attack2Hash = Animator.StringToHash(_attack2Bool);
        _attack3Hash = Animator.StringToHash(_attack3Bool);
        _shootHash = Animator.StringToHash(_shootTrigger);
    }

    public void SetXVelocity(float value)
    {
        if (_animator) _animator.SetFloat(_xVelocityHash, Mathf.Abs(value));
    }

    public void TriggerAgro()
    {
        if (_animator) _animator.SetTrigger(_triggerAgroHash);
    }

    public void TriggerPatrol()
    {
        if (_animator) _animator.SetTrigger(_triggerPatrolHash);
    }

    public void TriggerDeath()
    {
        if (_animator) _animator.SetTrigger(_deathHash);
    }

    public void SetAttack1(bool value)
    {
        if (_animator) _animator.SetBool(_attack1Hash, value);
    }

    public void SetAttack2(bool value)
    {
        if (_animator) _animator.SetBool(_attack2Hash, value);
    }

    public void SetAttack3(bool value)
    {
        if (_animator) _animator.SetBool(_attack3Hash, value);
    }

    public void TriggerShoot()
    {
        if (_animator) _animator.SetTrigger(_shootHash);
    }

    public bool IsInState(string stateName)
    {
        if (!_animator) return false;
        return _animator.GetCurrentAnimatorStateInfo(0).IsName(stateName);
    }

    public Animator Animator => _animator;

    public bool IsInSleep() => IsInState(_sleepState);
    public bool IsInAgroMovement() => IsInState(_agroMovementState);
    public bool IsInTriggerAgro() => IsInState(_triggerAgroState);
    public bool IsInTriggerPatrol() => IsInState(_triggerPatrolState);
    public bool IsInAttack1() => IsInState(_attack1State);
    public bool IsInAttack2() => IsInState(_attack2State);
    public bool IsInAttack3() => IsInState(_attack3State);
    public bool IsInShoot() => IsInState(_shootState);
}
