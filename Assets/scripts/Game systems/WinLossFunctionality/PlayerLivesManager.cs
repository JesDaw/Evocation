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
        
            var currentPlayer = playerSwitch.GetCurrentPlayerController();

            if (currentPlayer != null)
            {
                GameObject playerObject = currentPlayer.gameObject;

                if (playerObject != playerSwitch.GetCurrentPlayerController()?.gameObject)
                {
                    playerSwitch.RemovePlayer(playerObject);
                    Destroy(playerObject);
                }
            }

            if (LifeCount._Value > 0 && playerSwitch.GetCurrentPlayerController() != null)
            {
                playerSwitch.SwitchPlayer(default);
            }
        }
    }
}
