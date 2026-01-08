using UnityEngine;
using System.Collections;

/// <summary>
/// Manages building stats and ownership
/// Buildings can be: Enemy, Allies, or Blank (unclaimed amenities)
/// </summary>
public class BuildingStatsManager : MonoBehaviour
{
    public SpriteRenderer _Renderer;
    public Stats _Stats;    
    [Header("Building Types")]
    [SerializeField] ScriptableStats AllyBuilding;
    [SerializeField] ScriptableStats EnemyBuilding;
    
    [Header("Building Type")]
    [SerializeField] bool isAmenity = false; // Amenity vs Base building

    void Start()
    {
        // Amenities start unclaimed (CPUs ignore "Blank" tag)
        if (isAmenity)
        {
            gameObject.tag = "Blank";
        }
        else
        {
            // Bases start with a team
            SetupBuilding();
        }

        if (_Renderer == null)
        {
            _Renderer = gameObject.GetComponent<SpriteRenderer>();
            Debug.Log($"auto linked sprite renderer onto {gameObject.name}");
        }
    }

    void SetupBuilding()
    {
        // Set up building based on enemy flag
        if (_Stats._Enemy)
        {
            SwapBuilding(EnemyBuilding, true);
        }
        else
        {
            SwapBuilding(AllyBuilding, false);
        }
    }

    public void buildingDebug()
    {
        Debug.Log("<color=cyan> Home base DESTROYED</color>");
    }

    public void SetMax()
    {
        if (_Stats.scriptableStats == null) return;
        _Stats._MaxHealth = _Stats.scriptableStats._MaxHealth;
    }

    /// <summary>
    /// Swap building to new configuration
    /// </summary>
    public void SwapBuilding(ScriptableStats scrStats, bool isEnemy)
    {
        if (scrStats == null)
        {
            Debug.LogWarning($"Trying to swap {gameObject.name} to null ScriptableStats!");
            return;
        }

        // Assign ScriptableStats
        _Stats.scriptableStats = scrStats;
        _Stats._Enemy = isEnemy;

        // Set tag based on ownership
        if (isEnemy)
        {
            gameObject.tag = "Enemy";
        }
        else
        {
            gameObject.tag = "Allies";
        }

        // CPUs don't target buildings (they have their own separate targeting)
        // Buildings don't attack, so clear their target tags
        _Stats.targetTags.Clear();

        // Initialize stats
        SetMax();
        _Stats._CurrentHealth = scrStats._MaxHealth;
        _Stats._MoveSpeed = 0; // Buildings don't move
        _Stats._KnockBackHealth = scrStats._KnockBackMax;
        _Stats._KnockBackMax = scrStats._KnockBackMax;

        Debug.Log($"{gameObject.name} swapped to {(isEnemy ? "Enemy" : "Ally")} building");
    }

    /// <summary>
    /// Swap building ownership (for when captured)
    /// </summary>
    public void SwapAccordingToWho(bool IsEnemy)
    {
        if (IsEnemy)
        {
            SwapBuilding(EnemyBuilding, true);
        }
        else
        {
            SwapBuilding(AllyBuilding, false);
        }

        //delay is needed because status effect runs on update()
        StartCoroutine(ResetDestroyedAfterDelay());
    }

    /// <summary>
    /// Player claims the building (amenity)
    /// </summary>
    public void SwapToPlayer()
    {
        Debug.Log("player claimed building");
        SwapBuilding(AllyBuilding, false);
        StartCoroutine(ResetDestroyedAfterDelay());
    }

    private IEnumerator ResetDestroyedAfterDelay()
    {
        yield return new WaitForSeconds(0.5f); // wait for status effects and any preliminary attacks
        _Stats.SetDestroyed(false);
    }
}