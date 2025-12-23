using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class SwarmMinionSpawner : MonoBehaviour
{
    [Header("Internal Pool")]
    public Transform spawnParent;
    public int sortingOrderOffset = 5;

    [Header("Chaos")]
    [Range(0f, 60f)] public float startAngleJitterDeg = 20f;
    public float startKickSpeed = 2.0f;

    readonly Queue<MinionBrain> _pool = new();
    readonly List<MinionBrain>  _active = new();

    SwarmConfig _config;
    bool _isSpawningEnabled;
    float _nextSpawnAt;
    int _spawnIdxPhase;
    
    // Squad Logic
    bool _squadInAggro;
    float _squadForgetTimer;
    Transform _squadTarget;
    MinionBrain _currentAggressor;
    SpriteRenderer _swarmSR;

    public bool HasAggressor => _currentAggressor && _currentAggressor.gameObject.activeInHierarchy;
    public bool IsSquadAggro => _squadInAggro;
    public Transform SquadTarget => _squadTarget;

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
        CleanActive();
        
        // Squad State Management
        if (_squadInAggro)
        {
            if (_squadTarget == null)
            {
                // Target lost/destroyed immediately
                _squadInAggro = false; 
            }
            else
            {
                // Note: Minions should call ReportEnemySeen to keep timer reset.
                // If no one calls it, timer runs out.
                // However, we need to know if minions see the player.
                // MinionBrain will handle calling ReportEnemySeen logic.
                // Here we just decrement if no report comes in.
                
                // NOTE: To avoid complexity, let's assume ReportEnemySeen sets timer to max.
                // We just decrement here.
                _squadForgetTimer -= Time.deltaTime;
                if (_squadForgetTimer <= 0)
                {
                    _squadInAggro = false;
                    _squadTarget = null;
                    _currentAggressor = null;
                }
            }
        }

        UpdateSquadRoles();

        if (_isSpawningEnabled && _config != null)
        {
            TryTopUpWithInterval();
        }
    }

    public void ReportEnemySeen(Transform target)
    {
        if (target == null) return;
        
        _squadTarget = target;
        _squadInAggro = true;
        _squadForgetTimer = _config.aggroForgetSeconds;
    }

    void UpdateSquadRoles()
    {
        if (_active.Count == 0) return;

        if (!_squadInAggro)
        {
            // All Patrol
            foreach (var m in _active) m.SetRole(MinionBrain.Role.Patrol);
            _currentAggressor = null;
            return;
        }

        // We are in Aggro
        // Ensure one Aggressor
        if (_currentAggressor == null || !_currentAggressor.gameObject.activeInHierarchy)
        {
            AssignNewAggressor();
        }

        // Assign roles
        foreach (var m in _active)
        {
            if (m == _currentAggressor)
                m.SetRole(MinionBrain.Role.Aggressor);
            else
                m.SetRole(MinionBrain.Role.Support);
        }
    }

    void AssignNewAggressor()
    {
        if (_squadTarget == null) return;

        MinionBrain best = null;
        float bestD = float.MaxValue;

        foreach (var m in _active)
        {
            if (!m.gameObject.activeInHierarchy) continue;
            float d = Vector2.Distance(m.transform.position, _squadTarget.position);
            if (d < bestD) { bestD = d; best = m; }
        }

        _currentAggressor = best;
    }

    void CreateMinionInPool()
    {
        if (_config.minionPrefab == null) return;
        var go = Instantiate(_config.minionPrefab, spawnParent);
        var m = go.GetComponent<MinionBrain>();
        if (m)
        {
            m.Initialize(this);
            go.SetActive(false);
            _pool.Enqueue(m);
        }
    }

    public void EnableSpawning(bool enable)
    {
        _isSpawningEnabled = enable;
        if (enable)
        {
            if (Time.time > _nextSpawnAt)
                _nextSpawnAt = Time.time; 
        }
    }

    // Called by SwarmBrain when Swarm itself sees player
    public void SetAggroTarget(Transform target)
    {
        ReportEnemySeen(target);
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
            _nextSpawnAt = Time.time + _config.spawnInterval;
        }
    }

    MinionBrain GetFromPool()
    {
        if (_pool.Count == 0) CreateMinionInPool();
        if (_pool.Count == 0) return null;

        var m = _pool.Dequeue();
        
        // Detach from parent so Swarm scale (-1) doesn't flip minions
        m.transform.SetParent(null, false);
        
        m.gameObject.SetActive(true);
        m.OnSpawn();

        return m;
    }

    void SetupMinion(MinionBrain m, int index)
    {
        m.transform.position = transform.position;

        int n = Mathf.Max(1, _config.maxMinions);
        float baseDeg = (index % n) * (360f / n);
        float ang = (baseDeg + Random.Range(-startAngleJitterDeg, +startAngleJitterDeg)) * Mathf.Deg2Rad;
        
        // Initial kick if using physics, but with NavMesh we might warp or just set destination.
        // MinionBrain Start will pick it up.
        
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
        var list = new List<MinionBrain>(_active);
        for (int i = 0; i < list.Count; i++)
        {
            var m = list[i];
            if (!m) continue;
            // Assuming MinionBrain has a Kill/Die method
            m.Kill();
        }
        _active.Clear();
    }

    public void Release(MinionBrain m)
    {
        if (!m) return;

        int idx = _active.IndexOf(m);
        if (idx >= 0) _active.RemoveAt(idx);

        if (m == _currentAggressor) _currentAggressor = null;

        m.gameObject.SetActive(false);
        m.transform.SetParent(spawnParent, false); // Return to parent for tidiness
        _pool.Enqueue(m);
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
