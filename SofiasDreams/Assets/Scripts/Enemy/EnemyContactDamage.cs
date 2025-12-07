using UnityEngine;

public class EnemyContactDamage : MonoBehaviour
{
    [SerializeField] private int contactDamage = 1;
    [SerializeField] private LayerMask playerHurtboxLayers;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if ((playerHurtboxLayers.value & (1 << other.gameObject.layer)) == 0)
            return;

        var hb = other.GetComponent<Hurtbox2D>();
        var target = hb ? hb.Owner : null;
        if (target == null || !target.IsAlive)
            return;

        Vector2 point = other.ClosestPoint(transform.position);
        target.ApplyDamage(contactDamage, point, Vector2.zero, gameObject);
    }
}
