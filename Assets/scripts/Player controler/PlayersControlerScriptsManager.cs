using UnityEngine;


public class PlayersControlerScriptsManager : MonoBehaviour
{
    [SerializeField] bool _actionable = true;

     public void OnEventRaised()
     {
        _actionable = !_actionable;
        
        if (TryGetComponent<PlayerMovement>(out PlayerMovement movement))
        {
            movement.enabled = _actionable;
        }
        if (TryGetComponent<Player_Combat>(out Player_Combat combat))
        {
            combat.enabled = _actionable;
        }
     }
}
