using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    public static PlayerHealth Instance;
    public event System.Action<int> OnDamaged;

    [Header("Visuals / Feedback")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float flashInterval = 0.2f;
    [SerializeField] private PlayerDamageFeedback damageFeedback;

    [Header("Health & Heals")]
    [SerializeField] private int maxHP = 5;
    [SerializeField] private int maxHeals = 3;

    [Header("I-frames")]
    [SerializeField] private float invincibilityDuration = 3f;
    
    public bool IsAlive => currentHP > 0; 

    private float flashTimer = 0f;
    private bool isSpriteVisible = true;

    private bool isInvincible = false;
    private float invincibilityTimer = 0f;

    private int currentHP;
    private int currentHeals;
    private int killCount;

    public System.Action OnHealthChanged;
    public System.Action OnHealsChanged;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (!damageFeedback) damageFeedback = GetComponent<PlayerDamageFeedback>();
        
        currentHP = maxHP;
        currentHeals = maxHeals;
        killCount = 0;
    }

    private void Update()
    {
        // I-frames + мигание
        if (isInvincible)
        {
            invincibilityTimer -= Time.deltaTime;
            flashTimer -= Time.deltaTime;

            if (flashTimer <= 0f)
            {
                isSpriteVisible = !isSpriteVisible;
                if (spriteRenderer) spriteRenderer.enabled = isSpriteVisible;
                flashTimer = flashInterval;
            }

            if (invincibilityTimer <= 0f)
            {
                isInvincible = false;
                if (spriteRenderer) spriteRenderer.enabled = true;
            }
        }
    }
    
    public void ApplyDamage(int amount, Vector2 hitPoint, Vector2 hitNormal, GameObject source)
    {
        // Если есть компонент фидбека — он сам сделает TakeDamage + нокбэк/стан.
        if (damageFeedback)
        {
            // Предпочтительно бьём от позиции атакующего; если её нет — от точки хита.
            Vector2 srcPos = source ? (Vector2)source.transform.position : hitPoint;
            damageFeedback.ApplyDamageWithKnockback(amount, srcPos);
        }
        else
        {
            // Фолбэк на чистый урон без фидбека.
            TakeDamage(amount);
        }
    }

    public void TakeDamage(int damage)
    {
        if (isInvincible) return;
        int hpBefore = currentHP;
        currentHP = Mathf.Max(currentHP - damage, 0);
        if (currentHP < hpBefore)
        {
            OnDamaged?.Invoke(damage);
            isInvincible = true;
            invincibilityTimer = invincibilityDuration;
            OnHealthChanged?.Invoke();
            if (currentHP <= 0) Die();
        }
    }

    public bool CanHeal() => currentHeals > 0 && currentHP < maxHP;

    public void Heal()
    {
        if (!CanHeal()) return;
        currentHP = Mathf.Min(currentHP + 1, maxHP);
        currentHeals = Mathf.Max(0, currentHeals - 1);
        OnHealthChanged?.Invoke();
        OnHealsChanged?.Invoke();
    }

    public void RegisterKill()
    {
        killCount++;
        if (killCount >= 2)
        {
            killCount = 0;
            if (currentHeals < maxHeals)
            {
                currentHeals++;
                OnHealsChanged?.Invoke();
            }
        }
    }

    public void RestoreAtBonfire()
    {
        currentHP = maxHP;
        currentHeals = maxHeals;
        killCount = 0;
        OnHealsChanged?.Invoke();
        OnHealthChanged?.Invoke();
    }

    private void Die()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public int GetCurrentHP() => currentHP;
    public int GetMaxHP() => maxHP;
    public int GetCurrentHeals() => currentHeals;
    public int GetMaxHeals() => maxHeals;
    public bool IsInvincible => isInvincible;
}
