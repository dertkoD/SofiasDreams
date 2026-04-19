using UnityEngine;

/// <summary>
/// Dissolve bridge for the ReworkBugEnemyGround.
/// On enemy death, plays the dissolve effect on every assigned
/// <see cref="SpriteDissolveController"/> (head + body) at the same time.
/// Mirrors <see cref="EnemyDissolveBridge"/> but for multiple sprites.
/// </summary>
public class ReworkMultiSpriteDissolveBridge : MonoBehaviour
{
    [Header("References")]
    [Tooltip("All dissolve controllers that should play on death (one per sprite, e.g. Head + Body).")]
    [SerializeField] private SpriteDissolveController[] dissolveControllers;
    [SerializeField] private DissolveVfxSettingsSO dissolveSettings;

    [Header("Health source")]
    [Tooltip("If empty, will search the GameObject and its parents for a Health component.")]
    [SerializeField] private Health health;

    private bool _isDead;

    private void Awake()
    {
        if (dissolveControllers == null || dissolveControllers.Length == 0)
            dissolveControllers = GetComponentsInChildren<SpriteDissolveController>(true);

        if (!health)
        {
            health = GetComponent<Health>();
            if (!health) health = GetComponentInParent<Health>();

            if (!health)
            {
                var facade = GetComponentInParent<EnemyFacade>();
                if (facade) health = facade.Health;
            }
        }
    }

    private void OnEnable()
    {
        if (health != null)
            health.OnHealthChanged += OnHealthChanged;
    }

    private void OnDisable()
    {
        if (health != null)
            health.OnHealthChanged -= OnHealthChanged;
    }

    private void OnHealthChanged()
    {
        if (_isDead || health == null) return;

        if (health.CurrentHP <= 0)
        {
            _isDead = true;
            PlayDissolve();
        }
    }

    private void PlayDissolve()
    {
        if (dissolveControllers == null || dissolveControllers.Length == 0 || dissolveSettings == null)
        {
            Debug.LogWarning($"[ReworkMultiSpriteDissolveBridge] Missing references on {gameObject.name}. Controllers: {(dissolveControllers == null ? 0 : dissolveControllers.Length)}, Settings: {dissolveSettings}");
            return;
        }

        for (int i = 0; i < dissolveControllers.Length; i++)
        {
            var c = dissolveControllers[i];
            if (c) c.Play(dissolveSettings);
        }
    }
}
