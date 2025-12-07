using UnityEngine;

public class MuzzleFlashAutoDespawn : MonoBehaviour
{
    public void AnimEvent_DestroySelf() { Destroy(gameObject); }
}
