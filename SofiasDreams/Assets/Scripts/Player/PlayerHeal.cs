using UnityEngine;

public class PlayerHeal : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private KeyCode healKey = KeyCode.Q;

    [Header("Healing")]
    [SerializeField] private float holdDuration = 8.04f;
    [SerializeField] private string healBoolName = "isHealing";

    [Header("Refs (optional)")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private PlayerJump jump;
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerRangedAbility powershot; // respect lock

    public bool IsHealing { get; private set; }
    public float Progress01 => IsHealing ? _timer / holdDuration : 0f;

    private int _healBoolId;
    private float _timer;

    void Awake()
    {
        if (!playerHealth) playerHealth = PlayerHealth.Instance ?? FindObjectOfType<PlayerHealth>();
        if (!movement)     movement     = GetComponent<PlayerMovement>();
        if (!jump)         jump         = GetComponent<PlayerJump>();
        if (!animator)     animator     = GetComponentInChildren<Animator>();
        if (!powershot)    powershot    = GetComponentInChildren<PlayerRangedAbility>() ?? GetComponentInParent<PlayerRangedAbility>();

        _healBoolId = Animator.StringToHash(healBoolName);
    }

    void OnEnable()
    {
        if (playerHealth != null)
            playerHealth.OnDamaged += OnDamaged;
    }

    void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.OnDamaged -= OnDamaged;
    }

    void Update()
    {
        // Hard-stop during powershot lock
        if (powershot && powershot.IsShotLocked)
        {
            if (IsHealing) CancelHealing();
            if (animator) animator.SetBool(_healBoolId, false);
            return;
        }

        HandleHealInput();

        if (animator) animator.SetBool(_healBoolId, IsHealing);

        if (!IsHealing) return;

        _timer += Time.deltaTime;

        if (_timer >= holdDuration && Input.GetKey(healKey))
        {
            CompleteHeal();
        }
    }

    private void HandleHealInput()
    {
        if (Input.GetKeyDown(healKey)) TryBeginHealing();
        if (Input.GetKeyUp(healKey))   CancelHealing();
    }

    private bool TryBeginHealing()
    {
        if (IsHealing) return false;
        if (playerHealth == null) return false;
        if (powershot && powershot.IsShotLocked) return false;

        bool grounded = (jump != null && jump.IsGrounded);
        if (!grounded) return false;

        if (!playerHealth.CanHeal()) return false;

        IsHealing = true;
        _timer = 0f;

        if (movement) movement.SetMovementLocked(true);
        return true;
    }

    public void CancelHealing()
    {
        if (!IsHealing) return;

        IsHealing = false;
        _timer = 0f;

        if (movement) movement.SetMovementLocked(false);
    }

    private void CompleteHeal()
    {
        IsHealing = false;
        _timer = 0f;

        if (movement) movement.SetMovementLocked(false);

        if (playerHealth != null && playerHealth.CanHeal())
            playerHealth.Heal();
    }

    private void OnDamaged(int dmg)
    {
        CancelHealing();
    }
}
