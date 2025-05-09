using UnityEngine;

public class PlayersControlerScriptsManager : MonoBehaviour
{
    public bool _actionable;
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
        if (TryGetComponent<PlayerCombat>(out PlayerCombat combat))
        {
            combat.enabled = _actionable;
            combat.controllable = _actionable;
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} does not have Player_Combat component.");
        }

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
        if (TryGetComponent<PlayerCombat>(out PlayerCombat combat))
        {
            combat.enabled = _actionable;
            combat.controllable = _actionable;
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} does not have Player_Combat component.");
        }
    }

    void OnDestroy()
    {
        if (playerSwitch != null)
        {
         playerSwitch.RemovePlayer(this.gameObject);
        }
    }

}
