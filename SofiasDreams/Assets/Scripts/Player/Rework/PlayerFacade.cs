using UnityEngine;

public class PlayerFacade : MonoBehaviour
{
    public Transform cameraTarget;

    public Health Health { get; private set; }
    public Healer Healer { get; private set; }   
    public PlayerRangedAbility RangedAbility { get; private set; }

    void Awake()
    {
        Health        = GetComponentInChildren<Health>();
        Healer        = GetComponentInChildren<Healer>();
        RangedAbility = GetComponentInChildren<PlayerRangedAbility>();
    }
}
