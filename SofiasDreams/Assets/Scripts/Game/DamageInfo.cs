using UnityEngine;

public class DamageInfo
{
    public int amount;              
    public DamageType type;          
    public Transform source;         
    public Vector2 hitPoint;         
    public Vector2 hitNormal;
    
    public Vector2 impulse;           
    public float stunSeconds;          

    public bool bypassInvuln;          
    public bool isCritical;            

    public static DamageInfo FromHit(Transform src, int dmg, Vector2 point, Vector2 normal,
        Vector2 impulse, DamageType t = DamageType.Melee,
        bool bypass = false, bool crit = false, float stun = 0f)
    {
        return new DamageInfo {
            amount = dmg,
            type = t,
            source = src,
            hitPoint = point,
            hitNormal = normal,
            impulse = impulse,
            stunSeconds = stun,
            bypassInvuln = bypass,
            isCritical = crit
        };
    }

    public static DamageInfo Simple(Transform src, int dmg, Vector2 dir, float force,
        DamageType t = DamageType.Melee)
    {
        return new DamageInfo {
            amount = dmg,
            type = t,
            source = src,
            hitPoint = (Vector2)(src ? src.position : Vector2.zero),
            hitNormal = -dir.normalized,
            impulse = dir.normalized * force
        };
    }
}
