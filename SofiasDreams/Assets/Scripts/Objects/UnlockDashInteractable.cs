using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Zenject;
public class UnlockDashInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] bool oneShot = true;
    [SerializeField] WorldHintTextFade hint;
    [SerializeField] bool destroyHintOnUse = true;

    [Header("Light (Pulse)")]
    [SerializeField] Light2D spotLight2D;
    [SerializeField] float baseIntensity = 0.9f;
    [SerializeField] float pulseAmplitude = 0.45f;
    [SerializeField] float pulseSpeed = 2.0f;    
    [SerializeField] float fadeOutTime = 0.25f;   

    [Header("Dissolve")]
    [SerializeField] SpriteDissolveController _dissolveController;
    [SerializeField] DissolveVfxSettingsSO _dissolveSettings;

    IPlayerAbilities _abilities;
    SignalBus _bus;
    bool _used;

    Coroutine _fadeRoutine;

    [Inject]
    void Construct(IPlayerAbilities abilities, SignalBus bus)
    {
        _abilities = abilities;
        _bus = bus;
    }

    void Reset()
    {
        if (!spotLight2D) spotLight2D = GetComponentInChildren<Light2D>(true);
    }

    void Awake()
    {
        if (!spotLight2D) spotLight2D = GetComponentInChildren<Light2D>(true);

        if (spotLight2D)
        {
            spotLight2D.enabled = true;
            spotLight2D.intensity = 0f; 
        }
    }

    void Update()
    {
        if (!spotLight2D) return;

        if (_used) return;

        float s = (Mathf.Sin(Time.time * pulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
        float target = baseIntensity + pulseAmplitude * s;

        spotLight2D.intensity = Mathf.Lerp(spotLight2D.intensity, target, 12f * Time.deltaTime);
    }

    public bool CanInteract =>
        _abilities != null &&
        !_abilities.HasDash &&
        !(oneShot && _used);

    public string PromptText => CanInteract ? "Press F to learn Dash" : "";

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (!CanInteract) return;
        hint?.Show();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        hint?.Hide();
    }

    public void Interact(Transform interactor)
    {
        if (!CanInteract) return;

        _abilities.GrantDash();
        _bus.Fire(new DashUnlocked());

        _used = true;

        hint?.DisableForeverFade(destroyHintOnUse);

        StartFadeOutLight();

        var col = GetComponent<Collider2D>();
        if (col) col.enabled = false;

        if (_dissolveController != null && _dissolveSettings != null)
        {
            _dissolveController.Play(_dissolveSettings, () => Destroy(gameObject));
        }
        else
        {
            StartCoroutine(DestroyAfterFade());
        }
    }

    void StartFadeOutLight()
    {
        if (!spotLight2D) return;

        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        _fadeRoutine = StartCoroutine(FadeLightToZero(fadeOutTime));
    }

    IEnumerator FadeLightToZero(float duration)
    {
        if (!spotLight2D) yield break;

        duration = Mathf.Max(0.0001f, duration);
        float start = spotLight2D.intensity;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = t / duration;
            spotLight2D.intensity = Mathf.Lerp(start, 0f, k);
            yield return null;
        }

        spotLight2D.intensity = 0f;
        spotLight2D.enabled = false;
        _fadeRoutine = null;
    }

    IEnumerator DestroyAfterFade()
    {
        if (fadeOutTime > 0f) yield return new WaitForSeconds(fadeOutTime);
        Destroy(gameObject);
    }
}
