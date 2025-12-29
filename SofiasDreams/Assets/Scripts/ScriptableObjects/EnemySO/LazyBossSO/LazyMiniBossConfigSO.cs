using UnityEngine;

[CreateAssetMenu(fileName = "LazyMiniBossConfig", menuName = "Configs/Enemy/Lazy Mini Boss")]
public class LazyMiniBossConfigSO : ScriptableObject
{
    [Header("Patrol")]
    [Min(0f)] public float patrolSpeed = 2f;
    [Min(0f)] public float patrolPathSearchRadius = 100f;
    public bool loopPath = true;
    [Min(0.01f)] public float waypointArriveDistance = 0.2f;
    [Min(0f)] public float patrolWaitTime = 1f;

    [Header("Agro Movement")]
    [Min(0f)] public float agroRunSpeed = 4f;
    [Min(0f)] public float agroForgetSeconds = 3.0f;

    [Header("Combat")]
    [Min(0f)] public float closeRangeThreshold = 2.0f;
    [Min(0f)] public float meleeAttackCooldown = 2.0f;
    [Min(0f)] public float shootAttackCooldown = 3.0f;
    [Min(0f)] public float shootRangeMin = 5.0f;
    
    [Header("Projectile")]
    [Min(0f)] public float projectileSpeed = 10f;
    [Min(0f)] public int projectileDamage = 1;
    [Min(0f)] public float projectileLifeTime = 5f;
    public GameObject projectilePrefab;
}
