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
    Dictionary<GameObject, float> unitPower = new Dictionary<GameObject, float>();
    [SerializeField] ResourceSpawner[] resourceSpawners;
    [HideInInspector] public float totalAIIncentive = 0; 

    Collider2D zoneCollider;
    Vector3 Center => zoneCollider != null ? zoneCollider.bounds.center : transform.position;
    [SerializeField] bool DebugLogs;

    void Awake()
    {
        zoneCollider = GetComponent<Collider2D>();
    }
    void Start()
    {
        if (startingPlayer != null)
        {
            AddCharacterToList(startingPlayer);
        }
        foreach (var spawner in resourceSpawners) totalAIIncentive += spawner.AIWeight;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Allies") && !collision.CompareTag("Player") && !collision.CompareTag("Enemy")) return;
        AddCharacterToList(collision.gameObject);
    }

    void AddCharacterToList(GameObject gameObject)
    {
        GameObject obj = gameObject;
        float power = obj.GetComponent<Stats>().scriptableStats._CalculatedPower;
        unitPower[obj] = power;

        if (gameObject.CompareTag("Allies"))
        {
            if (!AllyUnits.Contains(obj)) AllyUnits.Add(obj);
            allyPower += power;
            if (DebugLogs) Debug.Log($"Adding ally to ally list. Ally Power = {allyPower}");
        }
        else if (gameObject.CompareTag("Player"))
        {
            if (!PlayerUnits.Contains(obj)) PlayerUnits.Add(obj);
            allyPower += power;
            if (DebugLogs) Debug.Log($"Adding player to player list. Ally Power = {allyPower}");
        }
        else if (gameObject.CompareTag("Enemy"))
        {
            if (!EnemyUnits.Contains(obj)) EnemyUnits.Add(obj);
            enemyPower += power;
            if (DebugLogs) Debug.Log($"Adding player to enemy list. EnemyPower = {enemyPower}");
        }
        if (DebugLogs) Debug.Log($"Ally power - Enemy power = {allyPower - enemyPower}");
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        GameObject obj = collision.gameObject;
        float power = unitPower.TryGetValue(obj, out var p) ? p : 0f;
        unitPower.Remove(obj);

        if (collision.CompareTag("Allies"))
        {
            if (AllyUnits.Remove(obj)) allyPower -= power;
        }
        else if (collision.CompareTag("Player"))
        {
            if (PlayerUnits.Remove(obj)) allyPower -= power;
        }
        else if (collision.CompareTag("Enemy"))
        {
            if (EnemyUnits.Remove(obj)) enemyPower -= power;
        }
    }

    public float ExpectedOutcome(ScriptableStats extraCharacter = null, bool useLaneSwitchingIncentives = false)
    {
        List<Stats> enemyTeam = EnemyUnits.Select(u => u.GetComponent<Stats>()).ToList();
        List<Stats> playerTeam = PlayerUnits.Select(u => u.GetComponent<Stats>()).ToList();
        playerTeam.AddRange(AllyUnits.Select(u => u.GetComponent<Stats>()));
        
        float result = ExpectedOutcomeCalculator.CalculateExpectedOutcome(enemyTeam, playerTeam, extraCharacter);
        if (useLaneSwitchingIncentives) result *= totalAIIncentive;
        return result;
        
    }

    public float CenterOfPower()
    {
        float totalPower = enemyPower + allyPower;
        if (totalPower <= 0f) return Center.x;

        float weightedPositionSum = 0f;
        weightedPositionSum += SumWeightedX(EnemyUnits);
        weightedPositionSum += SumWeightedX(PlayerUnits);
        weightedPositionSum += SumWeightedX(AllyUnits);

        return weightedPositionSum / totalPower;
    }

    float SumWeightedX(List<GameObject> units)
    {
        float sum = 0f;
        for (int i = 0; i < units.Count; i++)
        {
            sum += units[i].transform.position.x * unitPower[units[i]];
        }
        return sum;
    }
}