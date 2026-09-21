using UnityEngine;
using Evocation.Clans;

/// <summary>
/// Status effects that affect clan-wide or game mechanics
/// These need to be handled by a ClanManager or the gamemechanic manager so it doent work yet
/// </summary>
[CreateAssetMenu(fileName = "New Clan Effect", menuName = "Status Effects/Clan Effect")]
public class ClanStatusEffect : StatusEffect
{
    [Header("Clan Settings")]
    public ClansList affectedClan;

    [Header("Economic Modifiers")]
    public float moneyGenerationMultiplier = 1f;
    public float spawnCostMultiplier = 1f;
    public int moneyGenerationFlat = 0;

    [Header("Gameplay Modifiers")]
    public bool revealFogOfWar = false;
    public float globalSpeedMultiplier = 1f;

    [Header("Stacking")]
    [SerializeField] private bool allowStacking = false;

    public override void OnApply(Stats target)
    {

        Debug.Log($"Clan effect {effectName} applied for clan {affectedClan}");
    }

    public override void OnTick(Stats target, float deltaTime)
    {
    }

    public override void OnRemove(Stats target)
    {
        Debug.Log($"Clan effect {effectName} removed for clan {affectedClan}");
    }

    public override bool CanStack()
    {
        return allowStacking;
    }


    public void ApplyToClan(object clanManager)
    {

        Debug.Log($"Applying {effectName} to clan manager");
    }


    public void RemoveFromClan(object clanManager)
    {
        Debug.Log($"Removing {effectName} from clan manager");
    }
}