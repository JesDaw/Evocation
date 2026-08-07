using UnityEngine;
using System;

[Serializable]
public class EmptyState : LevelState
{
    [SerializeField] private string message = "Empty state entered";
    protected override void OnEnterState() => Debug.Log($"[EmptyState] {message}");
}