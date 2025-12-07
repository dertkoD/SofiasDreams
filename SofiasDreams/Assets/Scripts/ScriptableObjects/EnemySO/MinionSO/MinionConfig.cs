using UnityEngine;

[CreateAssetMenu(fileName = "MinionConfig", menuName = "Configs/Minion")]
public class MinionConfig : ScriptableObject
{
    [Header("Orbit")]
    public float orbitRadius = 2.0f;
    public float orbitSpeed = 90f; // град/сек

    [Header("Chase & Return")]
    public float detectRange = 9f;
    public float shootRange = 6f;
    public float maxChaseDistance = 12f;   // дальше — возвращаемся к рою
    public float moveSpeed = 3.5f;
    public float turnRate = 540f;

    [Header("Despawn")]
    public float noPlayerDespawnTime = 6f; // не видим игрока долго — исчезаем (если далеко)

    [Header("Shooting")]
    public float fireCooldown = 0.8f;
    //public ProjectileConfig projectile;
}
