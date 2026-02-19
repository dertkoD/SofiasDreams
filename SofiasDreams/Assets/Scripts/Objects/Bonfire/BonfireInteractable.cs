using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Zenject;

public class BonfireInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] string bonfireId = "bonfire_01";
    [SerializeField] Transform checkpoint;

    [Header("Light 2D")]
    [SerializeField] Light2D bonfireLight;
    [SerializeField] float baseIntensity = 1.1f;
    [SerializeField] float pulseAmplitude = 0.45f;
    [SerializeField] float pulseSpeed = 2.0f;   
    [SerializeField] float igniteTime = 0.35f;  

    IBonfireService _bonfire;

    bool _isLit;             
    float _ignite01 = 0f;   
    Coroutine _igniteRoutine;

    [Inject]
    public void Construct(IBonfireService bonfire) => _bonfire = bonfire;

    void Awake()
    {
        if (checkpoint == null)
            checkpoint = transform;

        if (!bonfireLight)
            bonfireLight = GetComponentInChildren<Light2D>(true);

        if (bonfireLight)
        {
            bonfireLight.enabled = false;
            bonfireLight.intensity = 0f;
        }
    }

    void Update()
    {
        if (!bonfireLight) return;
        if (!_isLit) return;

        float s = (Mathf.Sin(Time.time * pulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f;

        float target = (baseIntensity + pulseAmplitude * s) * _ignite01;

        bonfireLight.intensity = Mathf.Lerp(bonfireLight.intensity, target, 12f * Time.deltaTime);
    }

    public bool CanInteract => true;

    public string PromptText => _bonfire != null && _bonfire.IsResting
        ? "Press F to leave"
        : "Press F to rest";

    public void Interact(Transform interactor)
    {
        _bonfire.ToggleRest(bonfireId, checkpoint.position);

        if (!_isLit)
            IgniteLight();
    }

    void IgniteLight()
    {
        if (!bonfireLight) return;

        _isLit = true;

        if (_igniteRoutine != null) StopCoroutine(_igniteRoutine);
        _igniteRoutine = StartCoroutine(IgniteRoutine());
    }

    IEnumerator IgniteRoutine()
    {
        bonfireLight.enabled = true;
        bonfireLight.intensity = 0f;

        float t = 0f;
        float dur = Mathf.Max(0.0001f, igniteTime);

        _ignite01 = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            _ignite01 = Mathf.Clamp01(t / dur);
            yield return null;
        }

        _ignite01 = 1f;
        _igniteRoutine = null;
    }
}