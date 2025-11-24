using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class PlayerLivesManager : MonoBehaviour
{
    [SerializeField] public IntVeriable LifeCount;
    [SerializeField] public int MaxLives;
    [SerializeField] UnityEvent _loose_game;
    [SerializeField] ActivePlayer activePlayer;
    [SerializeField] PlayerSwitch playerSwitch;
    [SerializeField] PlayerLivesDisplay playerLivesDisplay;
    public bool canSpawnMore = true;
    public static PlayerLivesManager Instance { get; private set; }

    void Start()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void GainLife()
    {
        if (canSpawnMore)
        {
            LifeCount._Value++;
            playerLivesDisplay.UpdateTorchDisplay();
            if (LifeCount._Value >= MaxLives) 
            {
                canSpawnMore = false;
            }
        }
        else
        {
            Debug.LogWarning("Max Players reached");
        }
    }

    /// <summary>
    /// Called when a player dies. Pass the dead player's GameObject to remove it.
    /// </summary>
    /// <param name="deadPlayer">The GameObject of the player who died</param>
    public void LooseLife(GameObject deadPlayer)
    {
        LifeCount._Value--;
        canSpawnMore = true;
        playerLivesDisplay.UpdateTorchDisplay();

        if (LifeCount._Value <= 0)
        {
            // Game over
            _loose_game.Invoke();
        }
        else
        {
            // Remove the dead player from the list
            if (deadPlayer != null)
            {
                playerSwitch.RemovePlayer(deadPlayer);
            }
        }
    }

    /// <summary>
    /// Legacy method - tries to figure out which player died.
    /// Better to use LooseLife(GameObject deadPlayer) instead.
    /// </summary>
    public void LooseLife()
    {
        // This is called from player's onDeath event
        // The problem is we don't know WHICH player died
        var currentPlayer = activePlayer.GetCurrentPlayerController();
        
        if (currentPlayer != null)
        {
            LooseLife(currentPlayer.gameObject);
        }
        else
        {
            // Fallback if we can't determine the player
            LifeCount._Value--;
            canSpawnMore = true;
            playerLivesDisplay.UpdateTorchDisplay();

            if (LifeCount._Value <= 0)
            {
                _loose_game.Invoke();
            }
        }
    }
}