using UnityEngine;

public class GroundPatrolHitFlipRelay2D : MonoBehaviour
{
    public GroundPatrolMovement2D owner;
    public bool reactOnAttack = true; // наш Hitbox попал в PlayerHurtbox
    public bool reactOnGotHit = false; // наш Hurtbox задет PlayerHitbox

    void Awake() { if (!owner) owner = GetComponentInParent<GroundPatrolMovement2D>(); }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!owner) return;

        int otherLayer = other.gameObject.layer;
        bool attack = reactOnAttack && (owner.playerHurtboxLayers.value & (1 << otherLayer)) != 0;
        bool gotHit = reactOnGotHit && (owner.playerHitboxLayers.value  & (1 << otherLayer)) != 0;

        if (attack || gotHit) owner.FlipOnDamageContact();
    }
}
