using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currencyReward = 0;

    [Header("Lifecycle")]
    [SerializeField] private bool destroy = true;                    // true → Destroy, false → вернуть в пул
    [SerializeField] private GameObject rootToDestroy;               // если null → gameObject
    [SerializeField] private bool deferDespawnToAnimator = true;     // ждать мост смерти (рекомендуется)
    [SerializeField] private EnemyDeathAnimatorBridge deathBridge;   // мост анимации смерти

    private int currentHealth;
    private bool isDead;
    private bool deathFinalized;

    void Awake()
    {
        currentHealth = maxHealth;
        if (!rootToDestroy) rootToDestroy = gameObject;
        if (!deathBridge) deathBridge = GetComponentInParent<EnemyDeathAnimatorBridge>();
    }

    void OnEnable()
    {
        isDead = false;
        deathFinalized = false;
        currentHealth = maxHealth;

        var rb = GetComponent<Rigidbody2D>();
        if (rb) rb.linearVelocity = Vector2.zero;
    }

    public int GetHealth() => currentHealth;

    public void TakeDamage(int damage)
    {
        if (isDead) return;
        currentHealth -= damage;
        if (currentHealth <= 0) Die();
    }
    
    public void ManualRespawnReset()
    {
        isDead = false;
        deathFinalized = false;
        currentHealth = maxHealth;

        // включить коллайдеры
        var cols = GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < cols.Length; i++) if (cols[i]) cols[i].enabled = true;

        // восстановить физику
        var rb = GetComponent<Rigidbody2D>();
        if (rb)
        {
            rb.simulated = true;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        // вернуть аниматор из Death в начальное состояние
        var bridge = GetComponentInParent<EnemyDeathAnimatorBridge>();
        if (bridge) bridge.RestoreAfterRespawn(idleStateName: "Idle", idleTrigger: null);
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        // награды/статы по необходимости
        // CurrencyManager.Instance?.AddCurrency(currencyReward);

        if (deferDespawnToAnimator && deathBridge != null)
        {
            // только запускаем смерть. финализацию сделает AnimationEvent_DeathEnd в мосту
            deathBridge.TriggerDeath();
            return;
        }

        // запасной путь, если мост не подключён
        FinalizeDeath();
    }

    /// <summary>Финализация после анимации смерти. Вызывается только мостом.</summary>
    public void FinalizeDeath()
    {
        if (deathFinalized) return;
        deathFinalized = true;

        if (!destroy)
        {
            if (TryReturnToPool()) return;
            gameObject.SetActive(false);
            return;
        }

        Destroy(rootToDestroy ? rootToDestroy : gameObject);
    }

    // Удобный вызов, когда хозяин (Swarm) приказывает умереть с анимацией
    public void ForceDeathByOwner()
    {
        if (isDead) return;
        isDead = true;

        if (deferDespawnToAnimator && deathBridge != null)
        {
            deathBridge.TriggerDeath();
            return;
        }
        FinalizeDeath();
    }

    private bool TryReturnToPool()
    {
        var returners = GetComponents<IReturnToPool>();
        foreach (var r in returners) if (r != null && r.ReturnToPool(gameObject)) return true;

        var parents = GetComponentsInParent<IReturnToPool>(true);
        foreach (var r in parents) if (r != null && r.ReturnToPool(gameObject)) return true;

        var children = GetComponentsInChildren<IReturnToPool>(true);
        foreach (var r in children) if (r != null && r.ReturnToPool(gameObject)) return true;

        return false;
    }

}
