using UnityEngine;

public class Hurtbox2D : MonoBehaviour
{
    private IDamageable _owner;
    public IDamageable Owner => _owner ??= GetComponentInParent<IDamageable>();
}
