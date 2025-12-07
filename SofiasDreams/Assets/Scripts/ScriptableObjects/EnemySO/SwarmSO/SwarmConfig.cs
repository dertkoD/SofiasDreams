using UnityEngine;

[CreateAssetMenu(fileName = "SwarmConfig", menuName = "Configs/Swarm")]
public class SwarmConfig  : ScriptableObject
{
    [Header("Levitation")]
    public float hoverAmplitude = 0.4f;
    public float hoverFrequency = 0.7f;
    public float driftSpeed = 0.35f;

    [Header("Vision & Proximity")]
    public float proximityRadius = 10f;   // триггер «подлетел игрок» -> начинаем спавнить
    public float visionRadius = 8f;       // «вижу игрока» -> отлетаю

    [Header("Retreat")]
    public float desiredDistance = 6f;    // на какой дистанции держаться от игрока
    public float retreatSpeed = 2.0f;     // скорость отлёта
    public float maxRoamDistance = 8f;    // далеко от точки старта не улетать

    [Header("Spawning (minions)")]
    public int maxMinions = 3;
    public float spawnInterval = 1.0f;    // интервал выпуска при близости игрока
    public float respawnDelay = 2.0f;     // задержка перед замещением убитого миниона

    [Header("Damage")]
    public int contactDamage = 1;

    [Header("Refs")]
    public MinionConfig minionConfig;
    public GameObject minionPrefab; // квадрат с MinionController
}
