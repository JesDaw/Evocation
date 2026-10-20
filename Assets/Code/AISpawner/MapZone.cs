using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MapZone : MonoBehaviour
{
    public int ZoneNumber;
    [SerializeField] GameObject startingPlayer;
    [HideInInspector] public List<GameObject> EnemyUnits = new List<GameObject>();
    [HideInInspector] public List<GameObject> PlayerUnits = new List<GameObject>();
    [HideInInspector] public List<GameObject> AllyUnits = new List<GameObject>();

    float enemyPower = 0f;
    float allyPower = 0f;
    readonly Dictionary<GameObject, float> unitPower = new Dictionary<GameObject, float>();
    [SerializeField] ResourceSpawner[] resourceSpawners;
    [HideInInspector] public float totalAIIncentive = 0f;

    Collider2D zoneCollider;
    Vector3 Center => zoneCollider != null ? zoneCollider.bounds.center : transform.position;
    [SerializeField] bool DebugLogs;

    void Awake()
    {
        zoneCollider = GetComponent<Collider2D>();
    }

    void Start()
    {
        if (startingPlayer != null) AddCharacterToList(startingPlayer);

        if (resourceSpawners == null) return;
        foreach (var spawner in resourceSpawners)
        {
            if (spawner != null) totalAIIncentive += spawner.AIWeight;
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Allies") && !collision.CompareTag("Player") && !collision.CompareTag("Enemy")) return;
        AddCharacterToList(collision.gameObject);
    }

    void AddCharacterToList(GameObject obj)
    {
        if (obj == null || unitPower.ContainsKey(obj)) return;

        Stats stats = obj.GetComponent<Stats>();
        if (stats == null || stats.scriptableStats == null) return;

        float power = stats.scriptableStats._CalculatedPower;
        unitPower[obj] = power;

        if (obj.CompareTag("Allies"))
        {
            AllyUnits.Add(obj);
            allyPower += power;
            if (DebugLogs) Debug.Log($"Adding ally to ally list. Ally Power = {allyPower}");
        }
        else if (obj.CompareTag("Player"))
        {
            PlayerUnits.Add(obj);
            allyPower += power;
            if (DebugLogs) Debug.Log($"Adding player to player list. Ally Power = {allyPower}");
        }
        else if (obj.CompareTag("Enemy"))
        {
            EnemyUnits.Add(obj);
            enemyPower += power;
            if (DebugLogs) Debug.Log($"Adding enemy to enemy list. Enemy Power = {enemyPower}");
        }

        if (DebugLogs) Debug.Log($"Ally power - Enemy power = {allyPower - enemyPower}");
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        RemoveCharacter(collision.gameObject);
    }

    void RemoveCharacter(GameObject obj)
    {
        if (obj == null) return;
        float power = unitPower.TryGetValue(obj, out float storedPower) ? storedPower : 0f;
        unitPower.Remove(obj);

        if (AllyUnits.Remove(obj)) allyPower -= power;
        if (PlayerUnits.Remove(obj)) allyPower -= power;
        if (EnemyUnits.Remove(obj)) enemyPower -= power;
    }

    void PruneInvalidUnits()
    {
        // Rebuild the small cache from the current lists. This prevents destroyed or
        // invalid units from leaving stale power in CenterOfPower().
        EnemyUnits.RemoveAll(obj => obj == null || obj.GetComponent<Stats>() == null || obj.GetComponent<Stats>().scriptableStats == null);
        PlayerUnits.RemoveAll(obj => obj == null || obj.GetComponent<Stats>() == null || obj.GetComponent<Stats>().scriptableStats == null);
        AllyUnits.RemoveAll(obj => obj == null || obj.GetComponent<Stats>() == null || obj.GetComponent<Stats>().scriptableStats == null);

        unitPower.Clear();
        enemyPower = 0f;
        allyPower = 0f;
        RebuildPowerForList(EnemyUnits, false);
        RebuildPowerForList(PlayerUnits, true);
        RebuildPowerForList(AllyUnits, true);
    }

    void RebuildPowerForList(List<GameObject> units, bool isAlly)
    {
        foreach (GameObject obj in units)
        {
            Stats stats = obj.GetComponent<Stats>();
            float power = stats.scriptableStats._CalculatedPower;
            unitPower[obj] = power;
            if (isAlly) allyPower += power;
            else enemyPower += power;
        }
    }

    public float ExpectedOutcome(ScriptableStats extraCharacter = null, bool useLaneSwitchingIncentives = false, int extraCharacterCopies = 1)
    {
        PruneInvalidUnits();
        List<Stats> enemyTeam = GetValidStats(EnemyUnits);
        List<Stats> playerTeam = GetValidStats(PlayerUnits);
        playerTeam.AddRange(GetValidStats(AllyUnits));

        float result = ExpectedOutcomeCalculator.CalculateExpectedOutcome(enemyTeam, playerTeam, extraCharacter, extraCharacterCopies);
        if (useLaneSwitchingIncentives) result *= totalAIIncentive;
        return result;
    }

    List<Stats> GetValidStats(List<GameObject> units)
    {
        var result = new List<Stats>();
        foreach (GameObject obj in units)
        {
            if (obj == null) continue;
            Stats stats = obj.GetComponent<Stats>();
            if (stats != null && !stats._IsDead) result.Add(stats);
        }
        return result;
    }

    public float CenterOfPower()
    {
        PruneInvalidUnits();
        float totalPower = enemyPower + allyPower;
        if (totalPower <= 0f) return Center.x;

        float weightedPositionSum = SumWeightedX(EnemyUnits)
            + SumWeightedX(PlayerUnits)
            + SumWeightedX(AllyUnits);
        return weightedPositionSum / totalPower;
    }

    float SumWeightedX(List<GameObject> units)
    {
        float sum = 0f;
        foreach (GameObject obj in units)
        {
            if (obj != null && unitPower.TryGetValue(obj, out float power))
                sum += obj.transform.position.x * power;
        }
        return sum;
    }
}
