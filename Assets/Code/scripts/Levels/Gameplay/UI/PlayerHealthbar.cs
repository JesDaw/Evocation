using UnityEngine;
using DG.Tweening;

public class PlayerHealthbar : HealthBarBase
{
    public ActivePlayer ActivePlayer;

    int _maxHealth = 100;

    internal delegate void UpdateHealthDelegate(float value);
    internal delegate void UpdateMaxHealthDelegate(int value);

    internal UpdateHealthDelegate updateHealthIndicator;
    internal UpdateMaxHealthDelegate updateMaxHealthIndicator;

    protected override void Start()
    {
        base.SetStartHealth();
        Debug.Assert(ActivePlayer != null, "ActivePlayer reference missing on player healthbar object!");

        ActivePlayer.PlayerActivating += OnPlayerActivating;
        ActivePlayer.PlayerDeactivating += OnPlayerDeactivating;

        updateHealthIndicator = UpdateHealthVisual;
        updateMaxHealthIndicator = (x) => _maxHealth = x;

        if (ActivePlayer.CurrentPlayer != null)
            OnPlayerActivating(ActivePlayer.CurrentPlayer);
    }

    void UpdateHealthVisual(float currentHealth)
    {
        float ratio = _maxHealth > 0 ? (float)currentHealth / _maxHealth : 0f;
        AnimateHealthChange(ratio);
    }

    public override void UpdateHealth()
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

    internal void getPlayerStats(GameObject player, out Stats result)
    {
        result = null;

        if (player == null)
            player = ActivePlayer.CurrentPlayer;

        if (player != null && player.TryGetComponent(out Stats stats))
            result = stats;
    }

    internal void getPlayerStats(out Stats result)
    {
        getPlayerStats(null, out result);
    }

    internal void OnPlayerDeactivating(GameObject player)
    {
        getPlayerStats(player, out Stats stats);
        if (stats != null)
        {
            stats.OnDamage -= OnActivePlayerDamage;
            stats.OnDeath -= OnActivePlayerDeath;
        }
    }

    internal void OnPlayerActivating(GameObject player)
    {
        getPlayerStats(player, out Stats stats);
        if (stats != null)
        {
            stats.OnDamage += OnActivePlayerDamage;
            stats.OnDeath += OnActivePlayerDeath;
        }

        UpdateHealth();
    }

    internal void OnActivePlayerDamage()
    {
        getPlayerStats(out Stats stats);
        if (stats != null)
            updateHealthIndicator(stats._CurrentHealth);
    }

    internal void OnActivePlayerDeath()
    {
        updateHealthIndicator(0);
    }
}
