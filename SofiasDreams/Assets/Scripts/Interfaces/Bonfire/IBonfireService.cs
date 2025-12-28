using UnityEngine;

public interface IBonfireService
{
    bool IsResting { get; }
    bool HasCheckpoint { get; }

    void ToggleRest(string bonfireId, Vector3 bonfirePos);
    void RespawnPlayerAtCheckpoint();
    
    void SetCheckpoint(string id, Vector3 pos);
}