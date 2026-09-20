using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class AILaneSwitcher : MonoBehaviour
{ 
    [SerializeField] MapZone UpperZone;
    [SerializeField] MapZone MiddleZone;
    [SerializeField] MapZone LowerZone;
    public float netPositiveExpectedOutcomeDiscrepancyWindow = 0f;
    public float netNegativeExpectedOutcomeDiscrepancyWindow = 0f;
    public bool BeOptimal = false;
    [SerializeField] BoxCollider2D Collider;
    [SerializeField] bool DebugLogs = false;
    [SerializeField] ResourceSpawner[] resourceSpawners;
    float UpperZoneExpectedOutcome;
    float MiddleZoneExpectedOutcome;
    float LowerZoneExpectedOutcome;

    void Awake()
    {
        if (Collider == null) Collider = GetComponent<BoxCollider2D>();
        if (Collider == null) Debug.LogError("All forks need box coliders to know which characters it should effect");
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.gameObject.CompareTag("Enemy")) return;
        AssignLaneToCharacter(collision.gameObject);
        if (DebugLogs) Debug.Log($"{collision.gameObject.name} layer is now: {LayerMask.LayerToName(collision.gameObject.layer)}");
    }

    public void AssignLaneToCharacter(GameObject character)
    {
        List<float> expectedValues = new List<float>();
        List<string> LaneLayerNames = new List<string>();
        if (UpperZone != null) 
        {
            expectedValues.Add(UpperZone.ExpectedOutcome() * UpperZone.totalAIIncentive);
            LaneLayerNames.Add("Enemy/TopLane");
        }
        if (MiddleZone != null) 
        {
            expectedValues.Add(MiddleZone.ExpectedOutcome() * MiddleZone.totalAIIncentive);
            LaneLayerNames.Add("Enemy/MidLane");
        }
        if (LowerZone != null) 
        {
            expectedValues.Add(LowerZone.ExpectedOutcome() * LowerZone.totalAIIncentive);
            LaneLayerNames.Add("Enemy/BotLane");
        }

        float Lowest = expectedValues.Min(); 
        float Highest = expectedValues.Max(); 

        float netExpectedOutcome = 0f;
        foreach (var value in expectedValues) netExpectedOutcome += value;
        
        float discrepancyWindow = 0f;
        if (netExpectedOutcome >= 0) discrepancyWindow = netPositiveExpectedOutcomeDiscrepancyWindow;
        else discrepancyWindow = netNegativeExpectedOutcomeDiscrepancyWindow;

        BeOptimal = false; // this is here just because I dont have the optimal rought algorithm down yet
        if (!BeOptimal) // greedy path
        {
            if (Lowest - Highest > discrepancyWindow)
            {
                for (int i = 0; i < expectedValues.Count - 1; i++) 
                {
                    if (expectedValues[i] == Lowest) character.layer = LayerMask.NameToLayer(LaneLayerNames[i]);
                }
            }
            else
            {
                for (int i = 0; i < expectedValues.Count - 1; i++) 
                {
                    if (expectedValues[i] == Highest) character.layer = LayerMask.NameToLayer(LaneLayerNames[i]);
                }
            }
        }
        else
        {
            
        }
    }
}
