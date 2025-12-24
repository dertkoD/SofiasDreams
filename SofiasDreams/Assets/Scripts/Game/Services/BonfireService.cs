using UnityEngine;
using Zenject;

public class BonfireService : IBonfireService, IInitializable
{
    const string Pref_BonfireId = "checkpoint.bonfireId";
    const string Pref_BonfireX  = "checkpoint.x";
    const string Pref_BonfireY  = "checkpoint.y";
    const string Pref_BonfireZ  = "checkpoint.z";

    readonly SignalBus _bus;
    readonly IEnemyCombatGate _enemyCombatGate;

    bool _isResting;
    string _checkpointId;
    Vector3 _checkpointPos;
    bool _hasCheckpoint;

    public bool IsResting => _isResting;
    public bool HasCheckpoint => _hasCheckpoint;

    public BonfireService(SignalBus bus, IEnemyCombatGate enemyCombatGate)
    {
        _bus = bus;
        _enemyCombatGate = enemyCombatGate;
    }

    public void Initialize() => LoadCheckpoint();

    public void ToggleRest(string bonfireId, Vector3 bonfirePos)
    {
        if (_isResting) ExitRest();
        else EnterRest(bonfireId, bonfirePos);
    }

    void EnterRest(string bonfireId, Vector3 bonfirePos)
    {
        _isResting = true;

        SetCheckpoint(bonfireId, bonfirePos);

        _enemyCombatGate.SetBonfireSafe(true);

        // tell the scene to respawn enemies
        _bus.Fire(new BonfireEnemiesRespawnRequested());

        // tell the player to lock + restore (player will react to this signal)
        _bus.Fire(new BonfireRestStateChanged {
            IsResting = true,
            BonfireId = _checkpointId,
            Position  = _checkpointPos
        });
    }

    void ExitRest()
    {
        _isResting = false;

        _enemyCombatGate.SetBonfireSafe(false);

        _bus.Fire(new BonfireRestStateChanged {
            IsResting = false,
            BonfireId = _checkpointId,
            Position  = _checkpointPos
        });
    }

    public void RespawnPlayerAtCheckpoint()
    {
        if (!_hasCheckpoint) return;

        _isResting = false;
        _enemyCombatGate.SetBonfireSafe(false);

        // Respawn enemies and player via scene systems
        _bus.Fire(new BonfireEnemiesRespawnRequested());
        _bus.Fire(new BonfireRespawnRequested { Position = _checkpointPos });

        _bus.Fire(new PlayerRespawnedAtBonfire {
            BonfireId = _checkpointId,
            Position  = _checkpointPos
        });
    }

    void SetCheckpoint(string bonfireId, Vector3 pos)
    {
        _checkpointId = bonfireId;
        _checkpointPos = pos;
        _hasCheckpoint = true;

        PlayerPrefs.SetString(Pref_BonfireId, _checkpointId);
        PlayerPrefs.SetFloat(Pref_BonfireX, _checkpointPos.x);
        PlayerPrefs.SetFloat(Pref_BonfireY, _checkpointPos.y);
        PlayerPrefs.SetFloat(Pref_BonfireZ, _checkpointPos.z);
        PlayerPrefs.Save();

        _bus.Fire(new BonfireCheckpointChanged {
            BonfireId = _checkpointId,
            Position  = _checkpointPos
        });
    }

    void LoadCheckpoint()
    {
        if (!PlayerPrefs.HasKey(Pref_BonfireId))
        {
            _hasCheckpoint = false;
            return;
        }

        _checkpointId = PlayerPrefs.GetString(Pref_BonfireId, "");
        _checkpointPos = new Vector3(
            PlayerPrefs.GetFloat(Pref_BonfireX, 0),
            PlayerPrefs.GetFloat(Pref_BonfireY, 0),
            PlayerPrefs.GetFloat(Pref_BonfireZ, 0)
        );

        _hasCheckpoint = !string.IsNullOrEmpty(_checkpointId);
    }
}