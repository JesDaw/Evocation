using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthbar : MonoBehaviour
{
    [SerializeField] ActivePlayer ActivePlayer;
    [SerializeField] Slider _Slider;

    void Start()
    {
        Debug.Assert(ActivePlayer != null, "This should've already been set via the Editor!");

        ActivePlayer.PlayerActivating += OnPlayerActivating;
        ActivePlayer.PlayerDeactivating += OnPlayerDeactivating;
        if (ActivePlayer.CurrentPlayer != null)
        {
            OnPlayerActivating(ActivePlayer.CurrentPlayer);
        }
    }

    /// <summary>
    /// Returns a reference to a player's stats
    /// </summary>
    /// <param name="player">A player GameObject, 'null' defaults to the Active Player</param>
    /// <param name="result">Current stats</param>
    void getPlayerStats(GameObject player, out Stats result)
    {
        result = null;

        if (player == null)
        {
            player = ActivePlayer.CurrentPlayer;
        }

        if ((player != null) && player.TryGetComponent<Stats>(out Stats stats))
        {
            result = stats;
        }
        else
        {
            Debug.LogWarning($"Stats component not found on player {player}!");
        }
    }

    /// <summary>
    /// Get the active player stats
    /// </summary>
    /// <param name="result">Player Stats</param>
    void getPlayerStats(out Stats result)
    {
        getPlayerStats(null, out Stats stats);
        result = stats;
    }

    /// <summary>
    /// Set the HealthBar according to the active player stats
    /// </summary>
    public void UpdateHealth()
    {
        getPlayerStats(out Stats stats);
        if (stats != null)
        {
            // FIXME:  
            //    We need a stats._MaxHealth (or something equivalent) to
            //    set the slider with.
            //
            //    The Healthbar won't work properly without it when switching
            //    between players because the bar will appear full even when
            //    the newly active player is injured.
            _Slider.maxValue = stats._MaxHealth;
            _Slider.value = stats._CurrentHealth;
        }
    }

    /// <summary>
    /// Called when the active player is about to be switched
    /// </summary>
    /// <param name="player">The player about to be deactivated</param>
    void OnPlayerDeactivating(GameObject player)
    {
        Debug.Assert(player == ActivePlayer.CurrentPlayer);
        getPlayerStats(player, out Stats stats);
        if (stats != null)
        {
            stats.OnDamage -= OnActivePlayerDamage;
            stats.OnDeath -= OnActivePlayerDeath;
        }
    }

    /// <summary>
    /// Called when a player is 'activated'
    /// </summary>
    /// <param name="player">New, active player</param>
    void OnPlayerActivating(GameObject player)
    {
        Debug.Assert(player == ActivePlayer.CurrentPlayer);
        getPlayerStats(player, out Stats stats);
        if (stats != null)
        {
            stats.OnDamage += OnActivePlayerDamage;
            stats.OnDeath += OnActivePlayerDeath;
        }

        UpdateHealth();
    }

    /// <summary>
    /// Called when the currently active player takes damage so
    /// that the health bar can be updated
    /// </summary>
    void OnActivePlayerDamage()
    {
        getPlayerStats(out Stats stats);
        if (stats != null)
        {
            _Slider.value = stats._CurrentHealth;
        }
    }

    /// <summary>
    /// Called when the currently active player dies so the
    /// health bar can be updated.
    /// </summary>
    void OnActivePlayerDeath()
    {
        _Slider.value = 0;
    }
}
