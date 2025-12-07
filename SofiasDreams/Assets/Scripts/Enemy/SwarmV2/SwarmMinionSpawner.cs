using System.Collections.Generic;
using UnityEngine;

public class SwarmMinionSpawner : MonoBehaviour
{
    [Header("Detect")] public VisionCone2D swarmVision;

    [Header("Pooling")]
    public MinionOrbitBrain2D minionPrefab;
    [Min(1)] public int poolSize = 3;

    [Header("Spawn")]
    [Min(1)] public int minionsPerSwarm = 3;
    public Transform spawnParent;
    public int sortingOrderOffset = 5;
    [Min(0.05f)] public float spawnInterval = 0.6f;

    [Header("Chaos")]
    public bool randomizeDirection = false;
    [Range(0f, 1.5f)] public float radiusJitter = 0.5f;
    [Range(0f, 0.9f)] public float speedJitter  = 0.25f;
    [Min(0f)] public float startKickSpeed = 2.0f;
    [Range(0f, 60f)] public float startAngleJitterDeg = 20f;

    readonly Queue<MinionOrbitBrain2D> _pool = new();
    readonly List<MinionOrbitBrain2D>  _active = new();

    bool  _seeing;
    float _nextSpawnAt;
    int   _spawnIdxPhase;

    SpriteRenderer _swarmSR;

    MinionOrbitBrain2D _aggressor;
    Transform _lastSeenPlayer;                       // NEW

    public bool HasAggressor => _aggressor && _aggressor.gameObject.activeInHierarchy;
    public Transform AggressorTarget => HasAggressor ? _aggressor.CurrentAttackTarget : null;
    public Transform FallbackTarget => _lastSeenPlayer; // для саппортов/назначения NEW

    void OnValidate()
    {
        if (!spawnParent)
        {
            var child = transform.Find("spawn");
            if (child) spawnParent = child;
        }
    }

    void Awake()
    {
        if (!swarmVision) swarmVision = GetComponent<VisionCone2D>();
        _swarmSR = GetComponentInChildren<SpriteRenderer>();

        if (!spawnParent)
        {
            var go = new GameObject("spawn");
            go.transform.SetParent(transform, false);
            spawnParent = go.transform;
        }

        var existing = spawnParent.GetComponentsInChildren<MinionOrbitBrain2D>(true);
        foreach (var m in existing) if (m && !m.gameObject.activeSelf) _pool.Enqueue(m);

        int toCreate = Mathf.Max(0, poolSize - _pool.Count);
        for (int i = 0; i < toCreate; i++)
        {
            var m = Instantiate(minionPrefab, spawnParent);
            m.gameObject.SetActive(false);
            _pool.Enqueue(m);
        }
    }

    void Update()
    {
        if (swarmVision && swarmVision.TryGetClosestTarget(out var p))
        {
            _lastSeenPlayer = p;                     // NEW: обновляем цель всегда
            if (!_seeing) _nextSpawnAt = Time.time;
            _seeing = true;
        }
        else _seeing = false;

        AggressorHousekeeping();

        if (_seeing) TryTopUpWithInterval();
    }

    void TryTopUpWithInterval()
    {
        CleanActive();
        if (_active.Count >= minionsPerSwarm) return;
        if (Time.time < _nextSpawnAt) return;

        var m = GetFromPool();
        SetupMinion(m, _spawnIdxPhase++);
        _active.Add(m);

        if (_lastSeenPlayer && !HasAggressor) AssignSpecificAggressor(_lastSeenPlayer); // NEW

        _nextSpawnAt = Time.time + spawnInterval;
    }

    MinionOrbitBrain2D GetFromPool()
    {
        var m = _pool.Count > 0 ? _pool.Dequeue() : Instantiate(minionPrefab, spawnParent);
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
        if (obit)
        {
            var baseObit = minionPrefab ? minionPrefab.GetComponent<OrbitPatrol2D>() : null;
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

        int n = Mathf.Max(1, minionsPerSwarm);
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

    public void DespawnAll()
    {
        for (int i = _active.Count - 1; i >= 0; i--) Release(_active[i]);
        _active.Clear();
        _spawnIdxPhase = 0;
        _nextSpawnAt = Time.time;
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
    }

    public void Release(MinionOrbitBrain2D m)
    {
        if (!m) return;

        int idx = _active.IndexOf(m);
        if (idx >= 0) _active.RemoveAt(idx);

        bool wasAggressor = (m == _aggressor);
        if (wasAggressor) _aggressor = null;

        if (m.transform.parent != spawnParent) m.transform.SetParent(spawnParent, false);
        m.transform.localPosition = Vector3.zero;

        var rb = m.GetComponent<Rigidbody2D>();
        if (rb) rb.linearVelocity = Vector2.zero;

        m.gameObject.SetActive(false);
        _pool.Enqueue(m);

        if (wasAggressor) PromoteSupportToAggressor(); // NEW

        if (_seeing)
        {
            if (!HasAggressor && _lastSeenPlayer) AssignSpecificAggressor(_lastSeenPlayer);
            if (_active.Count < minionsPerSwarm) _nextSpawnAt = Mathf.Min(_nextSpawnAt, Time.time);
        }
    }

    public bool TryClaimAggressor(MinionOrbitBrain2D m, Transform player)
    {
        CleanActive();
        if (HasAggressor) return false;
        if (m == null || !_active.Contains(m) || !m.gameObject.activeInHierarchy) return false;
        _aggressor = m;
        _lastSeenPlayer = player;                    // NEW
        m.EnterAttackMode(player);
        return true;
    }

    void AggressorHousekeeping()
    {
        CleanActive();
        if (_aggressor != null && !_aggressor.gameObject.activeInHierarchy)
        {
            _aggressor = null;
            PromoteSupportToAggressor();            // NEW
        }
        if (_seeing && !HasAggressor && _lastSeenPlayer) AssignSpecificAggressor(_lastSeenPlayer);
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

    void PromoteSupportToAggressor()                 // NEW
    {
        if (_active.Count == 0) return;
        // если знаем игрока — назначаем ближайшего к нему
        if (_lastSeenPlayer) { AssignSpecificAggressor(_lastSeenPlayer); return; }

        // иначе попробуем спросить визор сейчас
        if (swarmVision && swarmVision.TryGetClosestTarget(out var p))
        {
            _lastSeenPlayer = p;
            AssignSpecificAggressor(_lastSeenPlayer);
        }
    }

    void CleanActive()
    {
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            var m = _active[i];
            if (m == null || !m.gameObject.activeInHierarchy) _active.RemoveAt(i);
        }
        if (_aggressor != null && !_aggressor.gameObject.activeInHierarchy) _aggressor = null;
    }
}
