using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class SwarmMinionSpawner : MonoBehaviour
{
    [Header("Refs")]
    // Vision is now handled by Brain
    // Config comes from Zenject now

    [Header("Internal Pool")]
    // We use the config for pool size now
    public Transform spawnParent;
    public int sortingOrderOffset = 5;

    [Header("Chaos")]
    public bool randomizeDirection = false;
    [Range(0f, 1.5f)] public float radiusJitter = 0.5f;
    [Range(0f, 0.9f)] public float speedJitter  = 0.25f;
    [Min(0f)] public float startKickSpeed = 2.0f;
    [Range(0f, 60f)] public float startAngleJitterDeg = 20f;

    readonly Queue<MinionOrbitBrain2D> _pool = new();
    readonly List<MinionOrbitBrain2D>  _active = new();

    SwarmConfig _config;
    bool _isSpawningEnabled;
    float _nextSpawnAt;
    int _spawnIdxPhase;
    Transform _currentTarget;
    SpriteRenderer _swarmSR;
    MinionOrbitBrain2D _aggressor;

    public bool HasAggressor => _aggressor && _aggressor.gameObject.activeInHierarchy;

    [Inject]
    public void Construct(SwarmConfig config)
    {
        _config = config;
    }

    void Awake()
    {
        _swarmSR = GetComponentInChildren<SpriteRenderer>();

        if (!spawnParent)
        {
            var go = new GameObject("spawn");
            go.transform.SetParent(transform, false);
            spawnParent = go.transform;
        }
    }

    void Start()
    {
        if (_config == null) return;

        // Pre-warm pool
        int toCreate = Mathf.Max(0, _config.poolInitialSize);
        for (int i = 0; i < toCreate; i++)
        {
            CreateMinionInPool();
        }
    }

    void Update()
    {
        AggressorHousekeeping();

        if (_isSpawningEnabled && _config != null)
        {
            TryTopUpWithInterval();
        }
    }

    void CreateMinionInPool()
    {
        if (_config.minionPrefab == null) return;
        var go = Instantiate(_config.minionPrefab, spawnParent);
        var m = go.GetComponent<MinionOrbitBrain2D>();
        if (m)
        {
            go.SetActive(false);
            _pool.Enqueue(m);
        }
    }

    public void EnableSpawning(bool enable)
    {
        _isSpawningEnabled = enable;
        if (enable)
        {
            // Reset timer so it can spawn immediately if cooldown passed
            if (Time.time > _nextSpawnAt)
                _nextSpawnAt = Time.time; 
        }
    }

    public void SetAggroTarget(Transform target)
    {
        _currentTarget = target;
        if (_currentTarget && !HasAggressor)
        {
             AssignSpecificAggressor(_currentTarget);
        }
    }

    void TryTopUpWithInterval()
    {
        CleanActive();
        if (_active.Count >= _config.maxMinions) return;
        if (Time.time < _nextSpawnAt) return;

        var m = GetFromPool();
        if (m != null)
        {
            SetupMinion(m, _spawnIdxPhase++);
            _active.Add(m);
            if (_currentTarget && !HasAggressor) AssignSpecificAggressor(_currentTarget);
            _nextSpawnAt = Time.time + _config.spawnInterval;
        }
    }

    MinionOrbitBrain2D GetFromPool()
    {
        if (_pool.Count == 0) CreateMinionInPool();
        if (_pool.Count == 0) return null;

        var m = _pool.Dequeue();
        if (m.transform.parent != spawnParent) m.transform.SetParent(spawnParent, false);
        m.gameObject.SetActive(true);

        var rb = m.GetComponent<Rigidbody2D>();
        if (rb) { rb.simulated = true; rb.linearVelocity = Vector2.zero; rb.angularVelocity = 0f; }

        var ec = m.GetComponentInChildren<EnemyController>(true);
        if (ec) ec.ManualRespawnReset();

        return m;
    }

    void SetupMinion(MinionOrbitBrain2D m, int index)
    {
        m.transform.position = transform.position;

        var obit = m.GetComponent<OrbitPatrol2D>();
        if (obit && _config.minionPrefab)
        {
            var baseObit = _config.minionPrefab.GetComponent<OrbitPatrol2D>();
            if (baseObit)
            {
                obit.radius = baseObit.radius;
                obit.tangentialSpeed = baseObit.tangentialSpeed;
                obit.clockwise = baseObit.clockwise;
            }
            obit.radius += Random.Range(-radiusJitter, +radiusJitter);
            obit.tangentialSpeed *= 1f + Random.Range(-speedJitter, +speedJitter);
            if (randomizeDirection) obit.clockwise = (Random.value < 0.5f);
        }

        int n = Mathf.Max(1, _config.maxMinions);
        float baseDeg = (index % n) * (360f / n);
        float ang = (baseDeg + Random.Range(-startAngleJitterDeg, +startAngleJitterDeg)) * Mathf.Deg2Rad;
        Vector2 kickDir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));

        var rb = m.GetComponent<Rigidbody2D>();
        if (rb) rb.linearVelocity = kickDir * startKickSpeed;

        RaiseSortingAboveSwarm(m.gameObject, sortingOrderOffset);
    }

    void RaiseSortingAboveSwarm(GameObject minion, int offset)
    {
        int baseOrder = _swarmSR ? _swarmSR.sortingOrder : 0;
        var srs = minion.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in srs)
        {
            if (!sr) continue;
            sr.sortingOrder = Mathf.Max(sr.sortingOrder, baseOrder + offset);
            if (_swarmSR) sr.sortingLayerID = _swarmSR.sortingLayerID;
        }
    }

    public void KillAllMinionsAnimated()
    {
        CleanActive();
        var list = new List<MinionOrbitBrain2D>(_active);
        for (int i = 0; i < list.Count; i++)
        {
            var m = list[i];
            if (!m) continue;
            var ec = m.GetComponentInChildren<EnemyController>(true);
            if (ec != null) ec.ForceDeathByOwner();
            else Release(m);
        }
        _active.Clear();
    }

    public void Release(MinionOrbitBrain2D m)
    {
        if (!m) return;

        int idx = _active.IndexOf(m);
        if (idx >= 0) _active.RemoveAt(idx);

        bool wasAggressor = (m == _aggressor);
        if (wasAggressor) _aggressor = null;

        m.gameObject.SetActive(false);
        _pool.Enqueue(m);

        if (wasAggressor) PromoteSupportToAggressor();
    }

    void AggressorHousekeeping()
    {
        CleanActive();
        if (_aggressor != null && !_aggressor.gameObject.activeInHierarchy)
        {
            _aggressor = null;
            PromoteSupportToAggressor();
        }
    }

    void AssignSpecificAggressor(Transform player)
    {
        if (player == null) return;
        MinionOrbitBrain2D best = null;
        float bestD = float.PositiveInfinity;
        for (int i = 0; i < _active.Count; i++)
        {
            var m = _active[i];
            if (!m || !m.gameObject.activeInHierarchy) continue;
            float d = Vector2.Distance(m.transform.position, player.position);
            if (d < bestD) { bestD = d; best = m; }
        }
        if (best != null)
        {
            _aggressor = best;
            best.EnterAttackMode(player);
        }
    }

    void PromoteSupportToAggressor()
    {
        if (_active.Count == 0) return;
        if (_currentTarget) AssignSpecificAggressor(_currentTarget);
    }

    void CleanActive()
    {
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            var m = _active[i];
            if (m == null || !m.gameObject.activeInHierarchy) _active.RemoveAt(i);
        }
    }
}
