using UnityEngine;

[CreateAssetMenu(fileName = "LazyMiniBossConfig", menuName = "Configs/Enemy/Lazy Mini Boss")]
public class LazyMiniBossConfigSO : ScriptableObject
{
    [Header("Patrol")]
    [Min(0f)] public float patrolSpeed = 2f;
    public bool loopPath = true;
    public bool canWalkInPatrol;
    [Min(0.01f)] public float waypointArriveDistance = 0.2f;

    [Header("Agro Movement")]
    [Min(0f)] public float agroRunSpeed = 4f;
    [Min(0f)] public float agroForgetSeconds = 3.0f;

    [Header("Combat")]
    [Min(0f)] public float closeRangeThreshold = 2.0f;
    [Min(0f)] public float meleeAttackCooldown = 2.0f;
    [Min(0f)] public float shootAttackCooldown = 3.0f;
    [Min(0f)] public float shootRangeMin = 5.0f;

    [Header("Shoot Projectile")]
    [Min(0f)] public float projectileSpeed = 10f;
    [Min(0f)] public int projectileDamage = 1;
    public GameObject projectilePrefab;
    [Min(1)] public int projectilePoolSize = 5;

    [Header("Attack3 Projectile")]
    [Min(0f)] public float attack3ProjectileSpeed = 10f;
    [Min(0f)] public int attack3ProjectileDamage = 1;
    public GameObject attack3ProjectilePrefab;
    [Min(1)] public int attack3ProjectilePoolSize = 5;
}
