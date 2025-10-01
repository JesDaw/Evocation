using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class PlayerHealthbar : MonoBehaviour
{
    [SerializeField] internal ActivePlayer ActivePlayer;
    [SerializeField] internal Image _HealthFillImage;
    [SerializeField] internal Image _HealthTrailImage;

    [SerializeField] private float _tweenDuration = 0.25f;
    [SerializeField] private float _trailDelay = 0.4f;

    internal delegate void UpdateHealth(int value);
    internal delegate void UpdateMaxHealth(int value);

    // Function called to update the Health indicator
    internal UpdateHealth updateHealthIndicator;

    // Function called to update the Max Health indicator
    internal UpdateMaxHealth updateMaxHealthIndicator;

    private int _maxHealth = 100;

    private void Awake()
    {
        updateHealthIndicator = (x) =>
        {
            float ratio = _maxHealth > 0 ? (float)x / _maxHealth : 0f;
            DOTween.To(() => _HealthFillImage.fillAmount,
                       value => _HealthFillImage.fillAmount = value,
                       ratio, _tweenDuration)
                   .SetEase(Ease.InOutSine);

            // animate trailing bar after delay
            DOTween.Sequence()
                   .AppendInterval(_trailDelay)
                   .Append(DOTween.To(() => _HealthTrailImage.fillAmount,
                                      v => _HealthTrailImage.fillAmount = v,
                                      ratio, _tweenDuration)
                                 .SetEase(Ease.InOutSine));
        };

        updateMaxHealthIndicator = (x) =>
        {
            _maxHealth = x;
        };
    }

    internal void Start()
    {
        Debug.Assert(ActivePlayer != null, "First active player needs to be set in the editor for player healthbar");

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
                Debug.LogWarning($"Stats component not found on player {player.name}!");
            }
        }
        else
        {
            Debug.LogError("PlayerHealthBar can't find active player");
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
            updateHealthIndicator(stats._CurrentHealth);
        }
        else
        {
            updateMaxHealthIndicator(1);
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
            updateHealthIndicator(stats._CurrentHealth);
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
