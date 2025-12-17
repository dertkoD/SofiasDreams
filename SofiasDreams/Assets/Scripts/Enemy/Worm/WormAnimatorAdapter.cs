using UnityEngine;

public class WormAnimatorAdapter : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] Animator _animator;

    [Header("Triggers")]
    [SerializeField] string _triggerAttack = "TriggerAttack";
    [SerializeField] string _spinningTrigger = "SpinningTrigger";
    [SerializeField] string _stunTrigger = "StunTrigger";
    [SerializeField] string _patrolTrigger = "PatrolTrigger";
    [SerializeField] string _patrolDeath = "PatrolDeath";
    [SerializeField] string _spinningDeath = "SpinningDeath"; 

    [Header("State Names")]
    [SerializeField] string _patrolState = "Patrol";
    [SerializeField] string _triggerState = "Trigger";
    [SerializeField] string _spinningState = "Spinning";
    [SerializeField] string _stunState = "Stun";

    int _triggerAttackHash;
    int _spinningTriggerHash;
    int _stunTriggerHash;
    int _patrolTriggerHash;
    int _patrolDeathHash;
    int _spinningDeathHash;

    void Awake()
    {
        if (!_animator) _animator = GetComponentInChildren<Animator>(true);

        _triggerAttackHash = Animator.StringToHash(_triggerAttack);
        _spinningTriggerHash = Animator.StringToHash(_spinningTrigger);
        _stunTriggerHash = Animator.StringToHash(_stunTrigger);
        _patrolTriggerHash = Animator.StringToHash(_patrolTrigger);
        _patrolDeathHash = Animator.StringToHash(_patrolDeath);
        _spinningDeathHash = Animator.StringToHash(_spinningDeath);
    }

    public void TriggerAttack() => _animator?.SetTrigger(_triggerAttackHash);
    public void TriggerSpinning() => _animator?.SetTrigger(_spinningTriggerHash);
    public void TriggerStun() => _animator?.SetTrigger(_stunTriggerHash);
    public void TriggerPatrol() => _animator?.SetTrigger(_patrolTriggerHash);
    public void TriggerPatrolDeath() => _animator?.SetTrigger(_patrolDeathHash);
    public void TriggerSpinningDeath() => _animator?.SetTrigger(_spinningDeathHash);
    
    public void ResetPatrolTrigger() => _animator?.ResetTrigger(_patrolTriggerHash);
    public void ResetAllTriggers()
    {
        if (!_animator) return;
        _animator.ResetTrigger(_triggerAttackHash);
        _animator.ResetTrigger(_spinningTriggerHash);
        _animator.ResetTrigger(_stunTriggerHash);
        _animator.ResetTrigger(_patrolTriggerHash);
    }

    public bool IsInPatrol() => IsState(_patrolState);
    public bool IsInTrigger() => IsState(_triggerState);
    public bool IsInSpinning() => IsState(_spinningState);
    public bool IsInStun() => IsState(_stunState);

    public bool IsStunFinished()
    {
        if (!_animator) return true;
        var stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
        
        // Если мы в стейте Stun -> проверяем время
        if (stateInfo.IsName(_stunState))
        {
            return stateInfo.normalizedTime >= 1.0f;
        }
        
        // Если мы НЕ в стейте Stun (а логика думает что должна быть), значит аниматор уже перешел дальше.
        // Считаем это как "Stun Finished".
        return true; 
    }

    bool IsState(string name)
    {
        if (!_animator) return false;
        return _animator.GetCurrentAnimatorStateInfo(0).IsName(name);
    }
}
