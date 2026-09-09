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

    public Transform PlayerBase;
    Vector3 PlayerBaseLocation;
    public Transform EnemyBase;
    Vector3 EnemyBaseLocation;
    [HideInInspector] public List<GameObject> allyUnits = new List<GameObject>();
    [HideInInspector] public List<GameObject> enemyUnits = new List<GameObject>();
    [SerializeField] string[] LayerNames = { "MidLane", "BotLane", "TopLane" };
    [SerializeField] UnityEvent WinGame;
    [SerializeField] UnityEvent LooseGame;
    [SerializeField] bool DebugLogs = false;
    public static UnitTracker Instance { get; private set; }

    List<GameObject>[] laneUnits;
    public float GetBaseDistance(float fallback = 100f)
    {
        return (PlayerBase != null && EnemyBase != null)
            ? Vector3.Distance(PlayerBase.position, EnemyBase.position)
            : fallback;
    }

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

        int laneIndex = LaneIndexForLayer(unit.layer);
        if (laneIndex >= 0 && !laneUnits[laneIndex].Contains(unit))
            laneUnits[laneIndex].Add(unit);
    }

    public void RemoveUnit(GameObject unit)
    {
        if (unit.CompareTag("Allies")) allyUnits.Remove(unit);
        else if (unit.CompareTag("Enemy")) enemyUnits.Remove(unit);

        int laneIndex = LaneIndexForLayer(unit.layer);
        if (laneIndex >= 0)
        {
            RemoveUnordered(laneUnits[laneIndex], unit);
        }
        else
        {
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

    public void UpdateUnitLane(GameObject unit, int newLaneIndex)
    {
        if (unit == null) return;
        if (newLaneIndex < 0 || newLaneIndex >= LaneLayerNames.Length)
        {
            Debug.LogError($"UpdateUnitLane: index {newLaneIndex} is out of range for LaneLayerNames.");
            return;
        }

        // Remove from whatever lane list it was previously cached under (if any).
        int oldLaneIndex = LaneIndexForLayer(unit.layer);
        if (oldLaneIndex >= 0)
            RemoveUnordered(laneUnits[oldLaneIndex], unit);

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

    public List<GameObject> FindAllUnitsWithLayer(string layer)
    {
        int laneIndex = Array.IndexOf(LaneLayerNames, layer);
        if (laneIndex < 0)
        {
            if (DebugLogs) Debug.LogWarning($"FindAllUnitsWithLayer: '{layer}' is not a known lane layer.");
            return new List<GameObject>();
        }

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

    public int GetTeamUnitCount(string team) // "Player" or "Enemy"
    {
        int count = 0;
        count += FindAllUnitsWithLayer($"{team}/TopLane").Count;
        count += FindAllUnitsWithLayer($"{team}/MidLane").Count;
        count += FindAllUnitsWithLayer($"{team}/BotLane").Count;
        return count;
    }

    public float GetZoneDominance(ZoneType zone)
    {
        string lane = LaneNameFor(zone);
        int enemyCount = FindAllUnitsWithLayer($"Enemy/{lane}").Count;
        int playerCount = FindAllUnitsWithLayer($"Player/{lane}").Count;
        int total = enemyCount + playerCount;

        if (total == 0) return 0.5f;
        return (float)enemyCount / total;
    }

    /// <summary>
    /// Finds the player unit closest to fromPosition across all lanes.
    /// Returns float.MaxValue if none found; power is the closest unit's power (0 if none).
    /// </summary>
    public float GetClosestPlayerUnitDistance(Vector3 fromPosition, out float power)
    {
        power = 0f;
        float closestDistance = float.MaxValue;

        foreach (string lane in new[] { "TopLane", "MidLane", "BotLane" })
        {
            foreach (GameObject unit in FindAllUnitsWithLayer($"Player/{lane}"))
            {
                if (unit == null) continue;

                float distance = Vector3.Distance(unit.transform.position, fromPosition);
                if (distance < closestDistance)
                {
                    closestDistance = distance;

                    Stats stats = unit.GetComponent<Stats>();
                    if (stats != null && stats.scriptableStats != null)
                        power = stats.scriptableStats._CalculatedPower;
                }
            }
        }

        return closestDistance;
    }

    string LaneNameFor(ZoneType zone)
    {
        switch (zone)
        {
            case ZoneType.Upper: return "TopLane";
            case ZoneType.Middle: return "MidLane";
            case ZoneType.Lower: return "BotLane";
            default: return "MidLane";
        }
    }
}