using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class PlayerLivesManager : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI LifeText;
    [SerializeField] IntVeriable LifeCount;
    [SerializeField] int MaxLives;
    [SerializeField] UnityEvent _loose_game;
    [SerializeField] PlayerSwitch playerSwitch;
    bool canSpawnMore = true;

    void Update()
    {
        LifeText.text = LifeCount._Value.ToString("0");
    }

    public bool GainLife()
    {
        if (canSpawnMore)
        {
            LifeCount._Value++;
            if (LifeCount._Value == MaxLives) canSpawnMore = false;
            return true;
        }
        else
        {
            Debug.LogWarning("Max Players reached");
            return false;
        }
    }

    public void LooseLife()
    {
        LifeCount._Value--;
        canSpawnMore = true;

        if (LifeCount._Value == 0)
        {
            _loose_game.Invoke();
        }
        else
        {
            // Get the current player who died
            var currentPlayer = playerSwitch.GetCurrentPlayerController();

            if (currentPlayer != null)
            {
                // Disable the controls of the current player and remove them from the list
                currentPlayer.DisableControls();
                playerSwitch.RemovePlayer(currentPlayer._PlayerID);
            }

            // Switch to the next player after the current one dies
            playerSwitch.SwitchPlayer(default);
        }
    }
}
