using UnityEngine;

public class DaggerParryTrigger : MonoBehaviour
{
    DaggerCombat _combat;

    void Awake()
    {
        _combat = GetComponentInParent<DaggerCombat>();
    }

    void OnTriggerEnter2D(Collider2D other) => TryParry(other);
    void OnTriggerStay2D(Collider2D other)  => TryParry(other);

    void TryParry(Collider2D other)
    {
        if (!_combat || !_combat.IsParrying) return;

        Transform enemy = ResolveEnemy(other);
        if (enemy == null) return;

        _combat.TryExecuteParry(enemy);
    }

    static Transform ResolveEnemy(Collider2D col)
    {
        var hb = col.GetComponent<Hurtbox2D>();
        if (hb != null && hb.Owner is MonoBehaviour mb)
            return mb.transform;

        var kb = col.GetComponentInParent<Knockback2D>();
        if (kb != null)
            return kb.transform;

        return null;
    }
}
