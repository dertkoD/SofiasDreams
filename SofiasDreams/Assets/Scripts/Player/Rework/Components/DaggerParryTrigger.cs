using UnityEngine;

public class DaggerParryTrigger : MonoBehaviour
{
    DaggerCombat _combat;
    bool _parried;

    void Awake()
    {
        _combat = GetComponentInParent<DaggerCombat>();
    }

    void OnEnable()
    {
        _parried = false;
        if (_combat) _combat.SetParrying(true);
    }

    void OnDisable()
    {
        if (_combat) _combat.SetParrying(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_parried) return;

        Transform enemy = ResolveEnemy(other);
        if (enemy == null) return;

        _parried = true;

        if (_combat && _combat.TryExecuteParry(enemy))
            gameObject.SetActive(false);
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
