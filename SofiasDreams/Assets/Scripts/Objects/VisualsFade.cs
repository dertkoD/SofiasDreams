using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class VisualsFade : MonoBehaviour
{
    [Header("Sprite Renderers")]
    [SerializeField] List<SpriteRenderer> _spriteRenderers = new();

    [Header("Fade Settings")]
    [SerializeField] float fadeInDuration  = 0.25f;
    [SerializeField] float fadeOutDuration = 0.2f;

    [Header("Trigger Mode")]
    [Tooltip("If true, listens to OnTriggerEnter2D/Exit2D and shows/hides itself.")]
    [SerializeField] bool useSelfTrigger = false; // requires a Trigger Collider2D

    // [Header("Behavior")]
    // [Tooltip("If true, disables the renderers when fully hidden (after fade).")]
    // [SerializeField] bool disableRenderersWhenHidden = true;

    bool _disabledForever;
    Coroutine _fadeRoutine;

    void Awake()
    {
        // Auto-collect all child sprite renderers if none assigned
        if (_spriteRenderers == null || _spriteRenderers.Count == 0)
        {
            _spriteRenderers = new List<SpriteRenderer>(GetComponentsInChildren<SpriteRenderer>(true));
        }
    }

    // ===== Self-contained mode =====
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!useSelfTrigger) return;
        if (!other.CompareTag("Player")) return;
        Hide(); // art should disappear when player enters
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!useSelfTrigger) return;
        if (!other.CompareTag("Player")) return;
        Show(); // reappear when player exits
    }

    // ===== External control mode =====
    public void Show()
    {
        if (_disabledForever) return;
        FadeTo(1f, fadeInDuration);
    }

    public void Hide()
    {
        if (_disabledForever) return;
        FadeTo(0f, fadeOutDuration);
    }

    // ===== Internals =====
    void FadeTo(float targetAlpha, float duration)
    {
        if (_spriteRenderers == null || _spriteRenderers.Count == 0) return;

        if (_fadeRoutine != null)
            StopCoroutine(_fadeRoutine);

        _fadeRoutine = StartCoroutine(FadeRoutine(targetAlpha, duration));
    }

    IEnumerator FadeRoutine(float target, float duration)
    {
        if (_spriteRenderers == null || _spriteRenderers.Count == 0)
            yield break;

        float start = GetCurrentAlpha();
        float time = 0f;

        if (duration <= 0f)
        {
            SetAlpha(target);
            yield break;
        }

        while (time < duration)
        {
            time += Time.deltaTime;
            float a = Mathf.Lerp(start, target, time / duration);
            SetAlpha(a);
            yield return null;
        }

        SetAlpha(target);
    }

    float GetCurrentAlpha()
    {
        // Take alpha from first valid renderer
        for (int i = 0; i < _spriteRenderers.Count; i++)
        {
            var r = _spriteRenderers[i];
            if (r == null) continue;
            return r.color.a;
        }
        return 1f;
    }

    void SetAlpha(float a)
    {
        if (_spriteRenderers == null) return;

        for (int i = 0; i < _spriteRenderers.Count; i++)
        {
            var r = _spriteRenderers[i];
            if (r == null) continue;

            var c = r.color;
            c.a = a;
            r.color = c;
        }
    }

    void SetRenderersEnabled(bool enabled)
    {
        if (_spriteRenderers == null) return;

        for (int i = 0; i < _spriteRenderers.Count; i++)
        {
            var r = _spriteRenderers[i];
            if (r == null) continue;
            r.enabled = enabled;
        }
    }
}
