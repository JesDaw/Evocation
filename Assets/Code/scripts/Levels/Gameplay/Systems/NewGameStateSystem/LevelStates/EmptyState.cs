using UnityEngine;

/// <summary>
/// Empty state that does nothing. Useful for testing or as a placeholder.
/// </summary>
[CreateAssetMenu(fileName = "State_Empty", menuName = "Level States/Empty State")]
public class EmptyState : LevelState
{
    [Header("Empty State")]
    [SerializeField] private string message = "Empty state entered";
    
    protected override void OnEnterState()
    {
        Debug.Log($"[EmptyState] {message}");
    }
}