using UnityEngine;

/// <summary>
/// Action for waiting/saving money
/// Useful when AI should accumulate resources
/// Now uses flexible consideration system!
/// </summary>
[CreateAssetMenu(fileName = "DoNothing", menuName = "AI/Actions/Do Nothing")]
public class DoNothingAction : AIAction
{
    [Header("Wait Message")]
    [Tooltip("What to log when AI waits")]
    public string waitMessage = "AI is saving money...";

    public override void Execute(AIContext context)
    {
        // Do nothing - just wait and accumulate money
        if (!string.IsNullOrEmpty(waitMessage))
        {
            Debug.Log($"{waitMessage} (Money: {context.GetCurrentMoney():F1}, Units: {context.GetAIUnitCount()})");
        }
    }

    // Always executable (AI can always choose to wait)
    public override bool CanExecute(AIContext context)
    {
        return true;
    }
}

/// <summary>
/// Example: How to set up DoNothing considerations
/// 
/// GOOD times to wait:
/// - When we have lots of units already
///   → Add consideration: AIUnitCount (curve: high when many units)
/// 
/// - When no immediate threat
///   → Add consideration: ClosestEnemyDistance (curve: high when far)
/// 
/// - When saving for something expensive
///   → Add consideration: Money (curve: medium at 50%, low at high money)
/// 
/// - When enemy base is almost dead
///   → Add consideration: EnemyBaseHealth (curve: high when low)
/// 
/// BAD times to wait:
/// - When enemies are at base
///   → Add consideration: ClosestEnemyDistance (curve: low when close, high when far)
/// 
/// The AI will naturally choose to wait when conditions favor it!
/// </summary>