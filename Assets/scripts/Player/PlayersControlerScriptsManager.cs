using UnityEngine;


public class PlayersControlerScriptsManager : MonoBehaviour
{
    [SerializeField] bool _actionable = true;

     public void EnagbleControls()
     {
        
        _actionable = true;
        
        if (TryGetComponent<PlayerMovement>(out PlayerMovement movement))
        {
            movement.enabled = _actionable;
            movement._controllable = _actionable;
        }
        if (TryGetComponent<Player_Combat>(out Player_Combat combat))
        {
            combat.enabled = _actionable;
            combat._controllable = _actionable;
        }
        Debug.Log(gameObject + "Controls enabled");
     }

     public void DisableControls()
     {
        _actionable = false;
        
        if (TryGetComponent<PlayerMovement>(out PlayerMovement movement))
        {
            movement.enabled = _actionable;
            movement._controllable = _actionable;
        }
        if (TryGetComponent<Player_Combat>(out Player_Combat combat))
        {
            combat.enabled = _actionable;
            combat._controllable = _actionable;
        }
        Debug.Log(gameObject + "Controls Disabled");
     }
}
