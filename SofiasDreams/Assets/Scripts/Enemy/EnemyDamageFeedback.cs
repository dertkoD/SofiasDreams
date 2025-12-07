using System.Collections;
using System.Linq;
using UnityEngine;

public class EnemyDamageFeedback : MonoBehaviour
{
    [Header("Refs")] [SerializeField] Rigidbody2D rb;
    [SerializeField] Animator animator;
    [SerializeField] Collider2D[] hurtboxes;
    [SerializeField] SpriteRenderer[] sprites;
    [SerializeField] Health _health;

    IExternalHitStunHost stunHost;

    [Header("Config")] [SerializeField] HitReactionConfig _hitConfig;

    public bool InHitStun { get; private set; }

    Color[] _origColors;
    bool[] _origEnabled;
    float _savedDrag;

    void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (!animator) animator = GetComponentInChildren<Animator>(true);
        if (!_health) _health = GetComponentInParent<Health>();

        if (sprites == null || sprites.Length == 0)
            sprites = GetComponentsInChildren<SpriteRenderer>(true);

        if (hurtboxes == null || hurtboxes.Length == 0)
            hurtboxes = GetComponentsInChildren<Collider2D>(true)
                .Where(c => c.isTrigger && c.enabled &&
                            c.gameObject.name.ToLower().Contains("hurt")).ToArray();

        stunHost = GetComponentInParent<IExternalHitStunHost>();

        _origColors = sprites.Select(s => s ? s.color : Color.white).ToArray();
        _origEnabled = sprites.Select(s => s && s.enabled).ToArray();
    }

    public void OnDamage(Vector2 sourcePos)
    {
        Debug.Log("[EnemyDamageFeedback] OnDamage, source=" + sourcePos);
        if (!InHitStun)
            StartCoroutine(HitCR(sourcePos));
    }

    IEnumerator HitCR(Vector2 sourcePos)
    {
        Debug.Log("[EnemyDamageFeedback] HitCR start");

        InHitStun = true;

        foreach (var h in hurtboxes)
            if (h)
                h.enabled = false;

        float prevAnimSpeed = animator ? animator.speed : 1f;
        if (animator) animator.speed = 0f;

        if (rb && _hitConfig != null)
        {
            float dir = Mathf.Sign(transform.position.x - sourcePos.x);
            if (dir == 0) dir = -1f;
            if (_hitConfig.zeroHorizontalBeforeKnockback)
                rb.linearVelocity = new Vector2(0f, Mathf.Min(rb.linearVelocity.y, 0f));
            _savedDrag = rb.linearDamping;
            rb.linearDamping = _hitConfig.dragDuringStun;
            rb.linearVelocity = new Vector2(_hitConfig.knockbackX * dir, _hitConfig.knockbackY);
        }

        if (stunHost != null)
            stunHost.ExternalHitStunActive = true;

        Debug.Log("[EnemyDamageFeedback] Start flash coroutine");
        StartCoroutine(FlashWhileInvincibleCR());

        float stun = _hitConfig != null ? _hitConfig.hitStun : 0.15f;
        if (stun > 0f)
            yield return new WaitForSeconds(stun);

        if (animator) animator.speed = prevAnimSpeed;
        if (rb) rb.linearDamping = _savedDrag;
        foreach (var h in hurtboxes)
            if (h)
                h.enabled = true;

        if (stunHost != null)
            stunHost.ExternalHitStunActive = false;

        InHitStun = false;
        Debug.Log("[EnemyDamageFeedback] HitCR end");
    }

    IEnumerator FlashWhileInvincibleCR()
    {
        Debug.Log("[EnemyDamageFeedback] FlashWhileInvincible start");
        yield return null;

        if (_hitConfig == null || sprites == null || sprites.Length == 0)
        {
            Debug.LogWarning("[EnemyDamageFeedback] No hitConfig or sprites for flashing");
            yield break;
        }

        float interval   = _hitConfig.flashInterval;
        if (interval <= 0f)
            interval = 0.05f;

        Color flashColor = _hitConfig.flashColor;
        bool  doToggle   = _hitConfig.useToggleBlinkIfWhite;

        int tick = 0;

        while (_health != null && _health.IsInvincible)
        {
            tick++;
            Debug.Log($"[EnemyDamageFeedback] Flash tick {tick}, IsInv={_health.IsInvincible}");

            if (doToggle)
            {
                SetEnabled(false);
            }
            else
            {
                SetColor(flashColor);
            }

            yield return new WaitForSeconds(interval);

            if (doToggle)
            {
                SetEnabled(true);
            }
            else
            {
                RestoreColors();
            }

            yield return new WaitForSeconds(interval);
        }

        RestoreColors();
        SetEnabled(true);

        Debug.Log("[EnemyDamageFeedback] FlashWhileInvincible end");
    }

    static bool ApproximatelyWhite(Color c) =>
        Mathf.Approximately(c.r, 1f) && Mathf.Approximately(c.g, 1f) && Mathf.Approximately(c.b, 1f);

    void SetColor(Color c)
    {
        for (int i = 0; i < sprites.Length; i++)
            if (sprites[i])
                sprites[i].color = c;
    }

    void RestoreColors()
    {
        for (int i = 0; i < sprites.Length; i++)
            if (sprites[i])
                sprites[i].color = _origColors[i];
    }

    void SetEnabled(bool on)
    {
        for (int i = 0; i < sprites.Length; i++)
            if (sprites[i])
                sprites[i].enabled = on;
    }
}