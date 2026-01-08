using UnityEngine;

/// <summary>
/// Action for spawning a specific unit
/// Now uses flexible consideration system!
/// </summary>
[CreateAssetMenu(fileName = "SpawnUnit", menuName = "AI/Actions/Spawn Unit")]
public class SpawnUnitAction : AIAction
{
    [Header("Unit to Spawn")]
    public ScriptableStats unitStats;

    [Header("Spawn Requirements")]
    [Tooltip("Minimum money required (hard requirement)")]
    public float minMoneyThreshold = 0f;
    
    [Tooltip("Can't spawn if we have this many or more units")]
    public int maxOwnUnits = 999;

    public override bool CanExecute(AIContext context)
    {
        // Must have the unit stats
        if (unitStats == null)
        {
            Debug.LogWarning($"SpawnUnitAction '{actionName}' has no unitStats assigned!");
            return false;
        }
        
        // Must have a spawner
        if (context.spawner == null)
        {
            Debug.LogWarning("No spawner in context!");
            return false;
        }
        
        // Must be able to afford it
        float currentMoney = context.GetCurrentMoney();
        if (currentMoney < unitStats._spawnCost)
            return false;
        
        // Must meet minimum threshold
        if (currentMoney < minMoneyThreshold)
            return false;
        
        // Check unit cap
        if (context.GetAIUnitCount() >= maxOwnUnits)
            return false;
        
        return true;
    }

    public override void Execute(AIContext context)
    {
        if (!CanExecute(context)) return;

        // Spawn the unit
        context.spawner.SpawnFromSpawner(unitStats);
        
        // Deduct cost using the money manager
        if (context.aiMoneyManager != null)
        {
            context.aiMoneyManager.SpendMoney(unitStats._spawnCost);
        }

        Debug.Log($"AI spawned {unitStats.name} (Cost: {unitStats._spawnCost})");
    }
}

/// <summary>
/// Example: Tank unit spawn action
/// HIGH utility when enemies are close (defensive)
/// Set up considerations like:
/// - ClosestEnemyDistance (curve: high when close, low when far)
/// - Money (curve: can only spawn if affordable)
/// - TimeElapsed (curve: more valuable as game progresses)
/// </summary>