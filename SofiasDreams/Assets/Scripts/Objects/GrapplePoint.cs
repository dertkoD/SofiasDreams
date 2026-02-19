using UnityEngine;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent]
public class GrapplePoint : MonoBehaviour
{
    [Header("Light2D")]
    [SerializeField] Light2D spotLight2D;

    [Header("Intensity")]
    [SerializeField] float candidateIntensity = 1.0f;
    [SerializeField] float latchedIntensity   = 1.0f;

    [Header("Fade")]
    [SerializeField] float fadeInTime  = 0.18f;
    [SerializeField] float fadeOutTime = 0.22f;

    bool _candidate;
    bool _latched;

    float _target;
    Coroutine _fade;

    void Awake()
    {
        if (!spotLight2D) spotLight2D = GetComponentInChildren<Light2D>(true);

        if (spotLight2D)
        {
            spotLight2D.enabled = true;  
            spotLight2D.intensity = 0f;  
        }

        _target = 0f;
    }

    public void SetCandidate(bool value)
    {
        _candidate = value;
        RecalcTarget();
    }

    public void SetLatched(bool value)
    {
        _latched = value;
        RecalcTarget();
    }

    void RecalcTarget()
    {
        float next =
            _latched ? latchedIntensity :
            _candidate ? candidateIntensity :
            0f;

        _target = next;

        if (!spotLight2D) return;

        if (_fade != null) StopCoroutine(_fade);
        _fade = StartCoroutine(FadeTo(_target));
    }

    System.Collections.IEnumerator FadeTo(float target)
    {
        float start = spotLight2D.intensity;
        float dur = target > start ? fadeInTime : fadeOutTime;
        dur = Mathf.Max(0.0001f, dur);

        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float a = t / dur;
            spotLight2D.intensity = Mathf.Lerp(start, target, a);
            yield return null;
        }

        spotLight2D.intensity = target;
        _fade = null;
    }
}
