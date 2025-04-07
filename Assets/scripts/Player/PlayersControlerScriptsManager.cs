using UnityEngine;

public class PlayersControlerScriptsManager : MonoBehaviour
{
    [SerializeField] bool _actionable = true;
    public int _PlayerID;
    [SerializeField] PlayerSwitch playerSwitch;

    public void EnableControls()
    {
        _actionable = true;

        // Enable PlayerMovement if it exists
        if (TryGetComponent<PlayerMovement>(out PlayerMovement movement))
        {
            movement.enabled = _actionable;
            movement._controllable = _actionable;
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} does not have PlayerMovement component.");
        }

        // Enable Player_Combat if it exists
        if (TryGetComponent<Player_Combat>(out Player_Combat combat))
        {
            combat.enabled = _actionable;
            combat._controllable = _actionable;
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} does not have Player_Combat component.");
        }

        Debug.Log($"{gameObject.name} controls enabled");
    }

    public void DisableControls()
    {
        _actionable = false;

        // Disable PlayerMovement if it exists
        if (TryGetComponent<PlayerMovement>(out PlayerMovement movement))
        {
            movement.enabled = _actionable;
            movement._controllable = _actionable;
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} does not have PlayerMovement component.");
        }

        // Disable Player_Combat if it exists
        if (TryGetComponent<Player_Combat>(out Player_Combat combat))
        {
            combat.enabled = _actionable;
            combat._controllable = _actionable;
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} does not have Player_Combat component.");
        }

        Debug.Log($"{gameObject.name} controls disabled");
    }

    void OnDestroy()
    {
        if (playerSwitch != null)
        {
         playerSwitch.RemovePlayer(this.gameObject);
        }
    }

}
