public interface IEnemyPersistenceService
{
    bool IsKilled(string spawnId);
    void MarkKilled(string spawnId);
    void ClearKilled(string spawnId); // optional
}