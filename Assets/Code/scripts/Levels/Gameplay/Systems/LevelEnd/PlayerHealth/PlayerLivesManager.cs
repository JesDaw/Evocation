using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class PlayerLivesManager : MonoBehaviour
{
    [SerializeField] public IntVeriable  LifeCount;
    [SerializeField] public int MaxLives;
    [SerializeField] UnityEvent _loose_game;
    [SerializeField] ActivePlayer activePlayer;
    [SerializeField] PlayerSwitch playerSwitch;
    [SerializeField] PlayerLivesDisplay playerLivesDisplay;
    public bool canSpawnMore = true;

    public void GainLife()
    {
        if (canSpawnMore)
        {
            LifeCount._Value++;
            playerLivesDisplay.UpdateTorchDisplay();
            if (LifeCount._Value == MaxLives) canSpawnMore = false;
        }
        else
        {
            Debug.LogWarning("Max Players reached");
        }
    }

    //referanced my player character "onDeath" event
    public void LooseLife()
    {
        LifeCount._Value--;
        canSpawnMore = true;
        playerLivesDisplay.UpdateTorchDisplay();

        if (LifeCount._Value == 0)
        {
            _loose_game.Invoke();
        }
        else
        {
            var currentPlayer = activePlayer.GetCurrentPlayerController();

            if (currentPlayer != null)
            {
                GameObject playerObject = currentPlayer.gameObject;

                if (playerObject != activePlayer.GetCurrentPlayerController()?.gameObject) // <-- I dont think this works the way I thought it would I need to relook at it
                {
                    playerSwitch.RemovePlayer(playerObject);
                }
            }
        }
    }
}
