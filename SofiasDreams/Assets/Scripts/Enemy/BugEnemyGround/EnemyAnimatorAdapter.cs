using UnityEngine;

public class EnemyAnimatorAdapter : MonoBehaviour
{
    [SerializeField] Animator _animator;
    [SerializeField] Health _health;

    [Header("Animator params")]
    [SerializeField] string _deadParam = "Death";

    int _speedHash;
    int _deadHash;

    bool _wasAlive = true;

    void Awake()
    {
        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();

        _deadHash = Animator.StringToHash(_deadParam);
    }

    void Update()
    {
        bool alive = (_health as IHealth)?.IsAlive ?? true;
        if (alive != _wasAlive)
        {
            _wasAlive = alive;
            _animator.SetTrigger(_deadHash);
        }
    }
}
