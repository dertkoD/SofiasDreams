using UnityEngine;

public class IdleFloat : MonoBehaviour
{
    [Header("Position")]
    [SerializeField] float amplitude = 0.15f;   // how far up/down
    [SerializeField] float frequency = 1.0f;    // how fast

    [Header("Rotation (Optional)")]
    [SerializeField] bool rotate = false;
    [SerializeField] float rotationAmplitude = 2f;
    [SerializeField] float rotationFrequency = 1f;

    Vector3 _startLocalPos;
    Quaternion _startLocalRot;
    float _timeOffset;

    void Awake()
    {
        _startLocalPos = transform.localPosition;
        _startLocalRot = transform.localRotation;

        // Small random offset so multiple objects don’t sync
        _timeOffset = Random.value * 10f;
    }

    void Update()
    {
        float t = Time.time + _timeOffset;

        // Vertical float
        float yOffset = Mathf.Sin(t * frequency * Mathf.PI * 2f) * amplitude;
        transform.localPosition = _startLocalPos + Vector3.up * yOffset;

        // Optional rotation sway
        if (rotate)
        {
            float z = Mathf.Sin(t * rotationFrequency * Mathf.PI * 2f) * rotationAmplitude;
            transform.localRotation = _startLocalRot * Quaternion.Euler(0f, 0f, z);
        }
    }
}
