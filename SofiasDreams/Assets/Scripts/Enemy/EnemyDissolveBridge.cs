using UnityEngine;

public class EnemyDissolveBridge : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteDissolveController dissolveController;
    [SerializeField] private DissolveVfxSettingsSO dissolveSettings;

    private Health _health;
    private bool _isDead;

    private void Awake()
    {
        if (!dissolveController) 
            dissolveController = GetComponent<SpriteDissolveController>();

        _health = GetComponent<Health>();
        if (!_health) _health = GetComponentInParent<Health>();
        
        // Try to find via EnemyFacade if not found directly
        if (!_health)
        {
            var facade = GetComponentInParent<EnemyFacade>();
            if (facade) _health = facade.Health;
        }
    }

    private void OnEnable()
    {
        if (_health != null)
        {
            _health.OnHealthChanged += OnHealthChanged;
        }
    }

    private void OnDisable()
    {
        if (_health != null)
        {
            _health.OnHealthChanged -= OnHealthChanged;
        }
    }

    private void OnHealthChanged()
    {
        if (_isDead || _health == null) return;

        if (_health.CurrentHP <= 0)
        {
            _isDead = true;
            PlayDissolve();
        }
    }

    private void PlayDissolve()
    {
        if (dissolveController != null && dissolveSettings != null)
        {
            dissolveController.Play(dissolveSettings);
        }
        else
        {
            Debug.LogWarning($"[EnemyDissolveBridge] Missing references on {gameObject.name}. Controller: {dissolveController}, Settings: {dissolveSettings}");
        }
    }
}
