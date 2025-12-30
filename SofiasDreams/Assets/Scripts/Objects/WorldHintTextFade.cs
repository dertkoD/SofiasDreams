using UnityEngine;
using System.Collections;
using TMPro;

public class WorldHintTextFade : MonoBehaviour
{
    [Header("Text")]
    [SerializeField] TMP_Text text;

    [Header("Fade Settings")]
    [SerializeField] float fadeInDuration  = 0.25f;
    [SerializeField] float fadeOutDuration = 0.2f;

    [Header("Trigger Mode")]
    [Tooltip("If true, this hint listens to OnTriggerEnter2D/Exit2D and shows/hides itself.")]
    [SerializeField] bool useSelfTrigger = false; // requires a Trigger Collider2D on this object (or same GO)

    bool _disabledForever;
    Coroutine _fadeRoutine;

    void Awake()
    {
        if (text == null)
            text = GetComponentInChildren<TMP_Text>();

        SetAlpha(0f);
    }

    // ===== Self-contained mode =====
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!useSelfTrigger) return;
        if (!other.CompareTag("Player")) return;
        Show();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!useSelfTrigger) return;
        if (!other.CompareTag("Player")) return;
        Hide();
    }

    // ===== External control mode (unlockers, etc.) =====
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

    /// <summary>
    /// Fade out, then disable or destroy this hint, and prevent it from ever showing again.
    /// </summary>
    public void DisableForeverFade(bool destroy = false)
    {
        if (_disabledForever) return;
        _disabledForever = true;

        if (_fadeRoutine != null)
            StopCoroutine(_fadeRoutine);

        _fadeRoutine = StartCoroutine(DisableForeverRoutine(destroy));
    }

    IEnumerator DisableForeverRoutine(bool destroy)
    {
        // Fade out using your normal duration
        float start = text != null ? text.color.a : 0f;
        float time = 0f;

        while (time < fadeOutDuration)
        {
            time += Time.deltaTime;
            float a = Mathf.Lerp(start, 0f, fadeOutDuration <= 0f ? 1f : time / fadeOutDuration);
            SetAlpha(a);
            yield return null;
        }

        SetAlpha(0f);

        if (destroy) Destroy(gameObject);
        else gameObject.SetActive(false);
    }

    // ===== Internals =====
    void FadeTo(float targetAlpha, float duration)
    {
        if (text == null) return;

        if (_fadeRoutine != null)
            StopCoroutine(_fadeRoutine);

        _fadeRoutine = StartCoroutine(FadeRoutine(targetAlpha, duration));
    }

    IEnumerator FadeRoutine(float target, float duration)
    {
        float start = text.color.a;
        float time  = 0f;

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

    void SetAlpha(float a)
    {
        if (text == null) return;
        var c = text.color;
        c.a = a;
        text.color = c;
    }
}
