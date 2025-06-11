using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthbar : MonoBehaviour
{
    [SerializeField] internal ActivePlayer ActivePlayer;
    [SerializeField] internal Slider _Slider;

    internal delegate void UpdateHealth(int value);
    internal delegate void UpdateMaxHealth(int value);

    // Function called to update the Health indicator
    internal UpdateHealth updateHealthIndicator;

    // Function called to update the Max Health indicator
    internal UpdateMaxHealth updateMaxHealthIndicator;

    internal PlayerHealthbar()
    {
        updateHealthIndicator = (x) => _Slider.value = x;
        updateMaxHealthIndicator = (x) => _Slider.maxValue = x;
    }

    internal void Start()
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
    internal void getPlayerStats(GameObject player, out Stats result)
    {
        result = null;

        if (player == null)
        {
            player = ActivePlayer.CurrentPlayer;
        }

        if (player != null)
        {
            if (player.TryGetComponent<Stats>(out Stats stats))
            {
                result = stats;
            }
            else
            {
                Debug.LogWarning($"Stats component not found on player {player}!");
            }
        }
    }

    /// <summary>
    /// Get the active player stats
    /// </summary>
    /// <param name="result">Player Stats</param>
    internal void getPlayerStats(out Stats result)
    {
        getPlayerStats(null, out Stats stats);
        result = stats;
    }

    /// <summary>
    /// Set the HealthBar according to the active player stats
    /// </summary>
    public void updateHealthStats()
    {
        getPlayerStats(out Stats stats);
        if (stats != null)
        {
            updateMaxHealthIndicator(stats._MaxHealth);
            updateHealthIndicator(stats._Health);
        }
        else
        {
            updateMaxHealthIndicator(0);
            updateHealthIndicator(0);
        }
    }

    /// <summary>
    /// Called when the active player is about to be switched
    /// </summary>
    /// <param name="player">The player about to be deactivated</param>
    internal void OnPlayerDeactivating(GameObject player)
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
    internal void OnPlayerActivating(GameObject player)
    {
        Debug.Assert(player == ActivePlayer.CurrentPlayer);
        getPlayerStats(player, out Stats stats);
        if (stats != null)
        {
            stats.OnDamage += OnActivePlayerDamage;
            stats.OnDeath += OnActivePlayerDeath;
        }

        updateHealthStats();
    }

    /// <summary>
    /// Called when the currently active player takes damage so
    /// that the health bar can be updated
    /// </summary>
    internal void OnActivePlayerDamage()
    {
        getPlayerStats(out Stats stats);
        if (stats != null)
        {
            updateHealthIndicator(stats._Health);
        }
    }

    /// <summary>
    /// Called when the currently active player dies so the
    /// health bar can be updated.
    /// </summary>
    internal void OnActivePlayerDeath()
    {
        updateHealthIndicator(0);
    }
}
