using UnityEngine;

public class Weapon : MonoBehaviour
{
    [SerializeField] private int attackDamage = 10;

    [SerializeField] private LayerMask enemyHurtboxLayers;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"Weapon trigger with {other.name}, layer = {other.gameObject.layer}");
        
        // Only react to configured hurtbox layers (enemies + shortcuts, etc.)
        if ((enemyHurtboxLayers.value & (1 << other.gameObject.layer)) == 0)
            return;

        var hb = other.GetComponent<Hurtbox2D>();
        
        Debug.Log($"Hurtbox: {hb}, owner: {hb?.Owner}");
        
        var target = hb ? hb.Owner : null;
        if (target == null || !target.IsAlive)
            return;

        Vector2 hitPoint  = other.ClosestPoint(transform.position);
        Vector2 hitNormal = ((Vector2)other.transform.position - (Vector2)transform.position).normalized;

        target.ApplyDamage(attackDamage, hitPoint, hitNormal, gameObject);

        Debug.Log($"Hit {target} for {attackDamage}");
    }
}
