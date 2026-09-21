using UnityEngine;
using System;

[Serializable]
public class CombatState : LevelState
{
    protected override void OnEnterState()
    {
        if (switchToPlayerControl && CameraControlSwitcher.Instance != null)
            CameraControlSwitcher.Instance.SwitchToPlayerControl();
    }
}