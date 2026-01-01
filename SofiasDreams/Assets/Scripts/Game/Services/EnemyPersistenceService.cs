using UnityEngine;

public class EnemyPersistenceService : IEnemyPersistenceService
{
    const string Prefix = "enemy.killed.";

#if UNITY_EDITOR
    // === Editor-only testing switches ===
    // If true: all enemies behave as "not killed" in editor (no permanent deaths).
    const bool DisablePersistenceInEditor = true;

    // If true: also prevent writing killed flags in editor (keeps prefs clean).
    const bool DisableWritesInEditor = true;
#endif

    public bool IsKilled(string spawnId)
    {
        if (string.IsNullOrEmpty(spawnId)) return false;

#if UNITY_EDITOR
        if (DisablePersistenceInEditor)
            return false;
#endif

        return PlayerPrefs.GetInt(Prefix + spawnId, 0) == 1;
    }

    public void MarkKilled(string spawnId)
    {
        if (string.IsNullOrEmpty(spawnId)) return;

#if UNITY_EDITOR
        if (DisableWritesInEditor)
            return;
#endif

        PlayerPrefs.SetInt(Prefix + spawnId, 1);
        PlayerPrefs.Save();
    }

    public void ClearKilled(string spawnId)
    {
        if (string.IsNullOrEmpty(spawnId)) return;

#if UNITY_EDITOR
        // Even if persistence is disabled, allow clearing explicitly (useful for debug tools).
#endif

        PlayerPrefs.DeleteKey(Prefix + spawnId);
        PlayerPrefs.Save();
    }
}