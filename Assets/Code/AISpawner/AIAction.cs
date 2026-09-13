using UnityEngine;

public static class AIAction
{
    #region spawning characters
    public static bool CanExecuteSpawnAction(ScriptableStats character)
    {
        if (character == null) return false;
        if (SpawnObjects.EnemyInstance == null) return false;
        if (!SpawnObjects.EnemyInstance.spawningEnabled) return false;
        if (character._spawnCost > Money.AIInstance.CurrentMoney) return false;
        return true;
    }

    public static void ExecuteSpawnAction(ScriptableStats character)
    {
        if (character == null)
        {
            Debug.LogWarning("[AI] Null unit in spawn sequence, skipping");
            return;
        }

        GameObject spawned = SpawnObjects.EnemyInstance.Spawn(character);

        if (spawned != null && AILevelHarness.Instance != null && AILevelHarness.Instance.showDebugLogs)
        {
            Debug.Log($"[AI] Spawned {character.name}");
        }
    }
    #endregion
}