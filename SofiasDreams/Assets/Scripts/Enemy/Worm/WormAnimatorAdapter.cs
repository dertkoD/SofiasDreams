using UnityEngine;

public class WormAnimatorAdapter : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Animator _animator;

    [Header("Triggers")]
    [SerializeField] private string _triggerAttack = "TriggerAttack";
    [SerializeField] private string _spinningTrigger = "SpinningTrigger";
    [SerializeField] private string _stunTrigger = "StunTrigger";
    [SerializeField] private string _patrolTrigger = "PatrolTrigger";
    [SerializeField] private string _patrolDeath = "PatrolDeath";
    [SerializeField] private string _spinningDeath = "SpinningDeath";

    [Header("State Names")]
    [SerializeField] private string _patrolState = "Patrol";
    [SerializeField] private string _triggerState = "Trigger";
    [SerializeField] private string _spinningState = "Spinning";
    [SerializeField] private string _stunState = "Stun";

    private int _triggerAttackHash;
    private int _spinningTriggerHash;
    private int _stunTriggerHash;
    private int _patrolTriggerHash;
    private int _patrolDeathHash;
    private int _spinningDeathHash;

    private bool _initialized;

    void Awake()
    {
        EnsureInitialized();
    }

    private void EnsureInitialized()
    {
        if (_initialized) return;

        if (!_animator)
            _animator = GetComponentInChildren<Animator>(true);

        ValidateTriggerName(_triggerAttack, nameof(_triggerAttack));
        ValidateTriggerName(_spinningTrigger, nameof(_spinningTrigger));
        ValidateTriggerName(_stunTrigger, nameof(_stunTrigger));
        ValidateTriggerName(_patrolTrigger, nameof(_patrolTrigger));
        ValidateTriggerName(_patrolDeath, nameof(_patrolDeath));
        ValidateTriggerName(_spinningDeath, nameof(_spinningDeath));

        _triggerAttackHash   = Animator.StringToHash(_triggerAttack);
        _spinningTriggerHash = Animator.StringToHash(_spinningTrigger);
        _stunTriggerHash     = Animator.StringToHash(_stunTrigger);
        _patrolTriggerHash   = Animator.StringToHash(_patrolTrigger);
        _patrolDeathHash     = Animator.StringToHash(_patrolDeath);
        _spinningDeathHash   = Animator.StringToHash(_spinningDeath);

        _initialized = true;
    }

    private void ValidateTriggerName(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            Debug.LogError($"[WormAnimatorAdapter] Trigger name is empty: {fieldName} on {name}", this);
    }

    private void SetTriggerSafe(int hash, string triggerNameForLog)
    {
        EnsureInitialized();

        if (!_animator) return;

        if (hash == 0)
        {
            Debug.LogError($"[WormAnimatorAdapter] Trigger hash is 0. Trigger name='{triggerNameForLog}' on {name}", this);
            return;
        }

        _animator.SetTrigger(hash);
    }

    private void ResetTriggerSafe(int hash, string triggerNameForLog)
    {
        EnsureInitialized();

        if (!_animator) return;

        if (hash == 0)
        {
            Debug.LogError($"[WormAnimatorAdapter] Reset trigger hash is 0. Trigger name='{triggerNameForLog}' on {name}", this);
            return;
        }

        _animator.ResetTrigger(hash);
    }

    public void TriggerAttack()        => SetTriggerSafe(_triggerAttackHash, _triggerAttack);
    public void TriggerSpinning()      => SetTriggerSafe(_spinningTriggerHash, _spinningTrigger);
    public void TriggerStun()          => SetTriggerSafe(_stunTriggerHash, _stunTrigger);
    public void TriggerPatrol()        => SetTriggerSafe(_patrolTriggerHash, _patrolTrigger);
    public void TriggerPatrolDeath()   => SetTriggerSafe(_patrolDeathHash, _patrolDeath);
    public void TriggerSpinningDeath() => SetTriggerSafe(_spinningDeathHash, _spinningDeath);

    public void ResetPatrolTrigger() => ResetTriggerSafe(_patrolTriggerHash, _patrolTrigger);

    public void ResetAllTriggers()
    {
        EnsureInitialized();
        if (!_animator) return;

        ResetTriggerSafe(_triggerAttackHash, _triggerAttack);
        ResetTriggerSafe(_spinningTriggerHash, _spinningTrigger);
        ResetTriggerSafe(_stunTriggerHash, _stunTrigger);
        ResetTriggerSafe(_patrolTriggerHash, _patrolTrigger);
    }

    public bool IsInPatrol()   => IsState(_patrolState);
    public bool IsInTrigger()  => IsState(_triggerState);
    public bool IsInSpinning() => IsState(_spinningState);
    public bool IsInStun()     => IsState(_stunState);

    public bool IsStunFinished()
    {
        EnsureInitialized();
        if (!_animator) return true;

        var stateInfo = _animator.GetCurrentAnimatorStateInfo(0);

        if (stateInfo.IsName(_stunState))
            return stateInfo.normalizedTime >= 1.0f;

        return true;
    }

    private bool IsState(string name)
    {
        EnsureInitialized();
        if (!_animator) return false;
        return _animator.GetCurrentAnimatorStateInfo(0).IsName(name);
    }
}
