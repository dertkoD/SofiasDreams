using UnityEngine;

public class LookAt2D : MonoBehaviour
{
    [Min(0f)] public float turnSpeedDegPerSec = 720f;
    public bool smooth = true;

    float _targetAngleDeg;
    bool _hasTarget;

    public void SetFacing(Vector2 worldDir)
    {
        if (worldDir.sqrMagnitude < 0.0001f) { _hasTarget = false; return; }
        _hasTarget = true;
        _targetAngleDeg = Mathf.Atan2(worldDir.y, worldDir.x) * Mathf.Rad2Deg;
    }

    void Update()
    {
        if (!_hasTarget) return;

        float current = transform.eulerAngles.z;
        float target  = _targetAngleDeg;

        float newZ = smooth
            ? Mathf.MoveTowardsAngle(current, target, turnSpeedDegPerSec * Time.deltaTime)
            : target;

        transform.rotation = Quaternion.Euler(0f, 0f, newZ);
    }
}
