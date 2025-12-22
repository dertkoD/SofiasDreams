using UnityEngine;

public class GrapplePointLock : MonoBehaviour
{
    [SerializeField] bool startLocked = true;
    public bool IsLocked { get; private set; }

    void Awake() => IsLocked = startLocked;

    public void Unlock() => IsLocked = false;
    public void Lock() => IsLocked = true;
}