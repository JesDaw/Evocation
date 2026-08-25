using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Stores all currently alive units in game and executes win/lose comparison logic.
/// Player characters live in PlayerSwitch.Instance.players; troop units are added/removed
/// by their spawners (no trigger colliders needed).
///
/// A unit's Layer encodes both team and lane, e.g. "Player/MidLane", "Enemy/TopLane".
///
/// Units are additionally cached per-lane in laneUnits (indexed by LaneLayerNames) so
/// lane queries are O(1) list lookups instead of filtering every unit every call.
/// SwitchLanes is responsible for keeping this cache in sync whenever a unit's lane changes,
/// via SetUnitLane.
/// </summary>
public class UnitTracker : MonoBehaviour
{
    /// <summary>
    /// Canonical team/lane layer names, in a fixed order. This is the single source of
    /// truth for lane layer strings — SwitchLanes indexes into this same array instead of
    /// keeping its own copy, so the two scripts can't drift out of sync on naming/casing.
    /// </summary>
    public static readonly string[] LaneLayerNames = new string[]
    {
        "Allies/TopLane", // 0
        "Allies/MidLane", // 1
        "Allies/BotLane", // 2

        "Enemy/TopLane",  // 3
        "Enemy/MidLane",  // 4
        "Enemy/BotLane",  // 5

        "Player/TopLane", // 6
        "Player/MidLane", // 7
        "Player/BotLane", // 8
    };

    [SerializeField] Transform PlayerBase;
    Vector3 PlayerBaseLocation;
    [SerializeField] Transform EnemyBase;
    Vector3 EnemyBaseLocation;
    [HideInInspector] public List<GameObject> allyUnits = new List<GameObject>();
    [HideInInspector] public List<GameObject> enemyUnits = new List<GameObject>();
    [SerializeField] string[] LayerNames = { "MidLane", "BotLane", "TopLane" };
    [SerializeField] UnityEvent WinGame;
    [SerializeField] UnityEvent LooseGame;
    [SerializeField] bool DebugLogs = false;
    public static UnitTracker Instance { get; private set; }

    // One list per entry in LaneLayerNames. Index i here always corresponds to LaneLayerNames[i].
    private List<GameObject>[] laneUnits;

    void Awake()
    {
        Instance = this;

        laneUnits = new List<GameObject>[LaneLayerNames.Length];
        for (int i = 0; i < laneUnits.Length; i++)
        {
            laneUnits[i] = new List<GameObject>();
        }
    }

    void Start()
    {
        PlayerBaseLocation = PlayerBase.position;
        EnemyBaseLocation = EnemyBase.position;
    }

    public void AddUnit(GameObject unit)
    {
        if (unit.CompareTag("Allies")) allyUnits.Add(unit);
        else if (unit.CompareTag("Enemy")) enemyUnits.Add(unit);

        // Register into whichever lane list matches its layer at spawn time.
        int laneIndex = LaneIndexForLayer(unit.layer);
        if (laneIndex >= 0 && !laneUnits[laneIndex].Contains(unit))
            laneUnits[laneIndex].Add(unit);
    }

    public void RemoveUnit(GameObject unit)
    {
        if (unit.CompareTag("Allies")) allyUnits.Remove(unit);
        else if (unit.CompareTag("Enemy")) enemyUnits.Remove(unit);

        // A unit's current layer always matches the one lane list it's cached in
        // (SetUnitLane keeps that invariant), so we can go straight to that single
        // list instead of scanning all 9 — important at ~20 units/sec of churn.
        int laneIndex = LaneIndexForLayer(unit.layer);
        if (laneIndex >= 0)
        {
            RemoveUnordered(laneUnits[laneIndex], unit);
        }
        else
        {
            // Layer didn't resolve to a known lane (shouldn't normally happen) —
            // fall back to scrubbing every list so we never leak a stale reference.
            for (int i = 0; i < laneUnits.Length; i++)
            {
                RemoveUnordered(laneUnits[i], unit);
            }
        }
    }

    /// <summary>
    /// Removes an item from a list in O(1) by swapping it with the last element and
    /// truncating, instead of List.Remove's O(n) shift of every following element.
    /// Safe here because lane lists are unordered membership sets — nothing relies
    /// on their element order.
    /// </summary>
    private static void RemoveUnordered(List<GameObject> list, GameObject unit)
    {
        int index = list.IndexOf(unit);
        if (index < 0) return;

        int lastIndex = list.Count - 1;
        list[index] = list[lastIndex];
        list.RemoveAt(lastIndex);
    }

    /// <summary>
    /// Moves a unit into a new lane: updates its actual Unity layer AND keeps the
    /// lane cache in sync. This is the method SwitchLanes should call instead of
    /// setting character.layer directly.
    /// </summary>
    /// <param name="unit">The unit changing lanes.</param>
    /// <param name="newLaneIndex">Index into LaneLayerNames for the target lane.</param>
    public void SetUnitLane(GameObject unit, int newLaneIndex)
    {
        if (unit == null) return;
        if (newLaneIndex < 0 || newLaneIndex >= LaneLayerNames.Length)
        {
            Debug.LogError($"SetUnitLane: index {newLaneIndex} is out of range for LaneLayerNames.");
            return;
        }

        int newLayer = LayerMask.NameToLayer(LaneLayerNames[newLaneIndex]);
        if (newLayer == -1)
        {
            Debug.LogError($"Layer '{LaneLayerNames[newLaneIndex]}' not found! Check Project Settings > Tags and Layers.");
            return;
        }

        // Remove from whatever lane list it was previously cached under (if any).
        int oldLaneIndex = LaneIndexForLayer(unit.layer);
        if (oldLaneIndex >= 0)
            RemoveUnordered(laneUnits[oldLaneIndex], unit);

        unit.layer = newLayer;

        if (!laneUnits[newLaneIndex].Contains(unit))
            laneUnits[newLaneIndex].Add(unit);
    }

    private int LaneIndexForLayer(int unityLayer)
    {
        string layerName = LayerMask.LayerToName(unityLayer);
        return Array.IndexOf(LaneLayerNames, layerName);
    }

    public int GetUnitCountByTag(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return 0;

        if (tag == "Allies")
        {
            if (DebugLogs) Debug.Log($"allyUnits.count = {allyUnits.Count}");
            return allyUnits.Count;
        }
        else if (tag == "Enemy")
        {
            if (DebugLogs) Debug.Log($"enemyUnits.count = {enemyUnits.Count}");
            return enemyUnits.Count;
        }

        return 0;
    }

    /// <summary>
    /// Returns the cached list of units on a given lane layer (e.g. "Player/MidLane").
    /// O(1) lookup, no per-call allocation — the returned list is the live cache, so
    /// treat it as read-only; don't Add/Remove from it directly.
    ///
    /// Falls back to scanning PlayerSwitch.Instance.players for Player/* layers, to
    /// cover player characters that haven't passed through a SwitchLanes trigger yet
    /// (and so were never registered into the cache).
    /// </summary>
    public List<GameObject> FindAllUnitsWithLayer(string layer)
    {
        int laneIndex = Array.IndexOf(LaneLayerNames, layer);
        if (laneIndex < 0)
        {
            if (DebugLogs) Debug.LogWarning($"FindAllUnitsWithLayer: '{layer}' is not a known lane layer.");
            return new List<GameObject>();
        }

        // Clean up anything destroyed since the last query.
        laneUnits[laneIndex].RemoveAll(unit => unit == null);

        if (layer.StartsWith("Player/") && PlayerSwitch.Instance != null)
        {
            int layerMask = LayerMask.NameToLayer(layer);
            foreach (GameObject player in PlayerSwitch.Instance.players)
            {
                if (player != null && player.layer == layerMask && !laneUnits[laneIndex].Contains(player))
                    laneUnits[laneIndex].Add(player);
            }
        }

        return laneUnits[laneIndex];
    }

    public void CompareUnitProximityToBases()
    {
        int playerScore = 0;
        foreach (string laneName in LayerNames)
        {
            if (CompareProximityDifferenceByLane(laneName)) playerScore++;
            else playerScore--;
        }

        if (playerScore > 0) WinGame?.Invoke();
        else LooseGame?.Invoke();
    }

    /// <summary>
    /// Returns true if the player's closest unit in this lane (troops + live
    /// player characters) has pushed further toward the enemy base than the
    /// enemy's closest unit has pushed toward the player base.
    /// </summary>
    bool CompareProximityDifferenceByLane(string lane)
    {
        float allyPush = 0f;
        GameObject closestAlly = FindClosestUnit($"Player/{lane}", EnemyBaseLocation);
        if (closestAlly != null)
            allyPush = Vector3.Distance(closestAlly.transform.position, EnemyBaseLocation);

        float enemyPush = 0f;
        GameObject closestEnemy = FindClosestUnit($"Enemy/{lane}", PlayerBaseLocation);
        if (closestEnemy != null)
            enemyPush = Vector3.Distance(closestEnemy.transform.position, PlayerBaseLocation);

        float difference = enemyPush - allyPush;
        return difference > 0f;
    }

    public GameObject FindClosestUnit(string unitLayer, Vector3 location)
    {
        List<GameObject> unitList = FindAllUnitsWithLayer(unitLayer);
        if (unitList.Count == 0) return null;

        GameObject closest = null;
        float minDistance = Mathf.Infinity;

        foreach (GameObject unit in unitList)
        {
            if (unit == null) continue;
            float distance = Vector3.Distance(location, unit.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closest = unit;
            }
        }
        return closest;
    }
}