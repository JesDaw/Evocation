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

    void Awake()
    {
        if (Collider == null) Collider = GetComponent<BoxCollider2D>();
        if (Collider == null) Debug.LogError("All forks need box colliders to know which characters it should affect");
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
        List<string> laneLayerNames = new List<string>();

        if (UpperZone != null) 
        {
            expectedValues.Add(UpperZone.ExpectedOutcome() * (1 + UpperZone.totalAIIncentive));
            laneLayerNames.Add("Enemy/TopLane");
        }
        if (MiddleZone != null) 
        {
            expectedValues.Add(MiddleZone.ExpectedOutcome() * (1 + MiddleZone.totalAIIncentive));
            laneLayerNames.Add("Enemy/MidLane");
        }
        if (LowerZone != null) 
        {
            expectedValues.Add(LowerZone.ExpectedOutcome() * (1 + LowerZone.totalAIIncentive));
            laneLayerNames.Add("Enemy/BotLane");
        }

        if (expectedValues.Count == 0) return;

        float lowest = expectedValues.Min(); 
        float highest = expectedValues.Max(); 

        float netExpectedOutcome = expectedValues.Sum();
        if (DebugLogs) Debug.Log($"lane expected outcomes (negative = AI favor): lowest = {lowest}, highest = {highest}, net = {netExpectedOutcome}");
        
        float discrepancyWindow = netExpectedOutcome >= 0 
            ? netPositiveExpectedOutcomeDiscrepancyWindow 
            : netNegativeExpectedOutcomeDiscrepancyWindow;

        float laneSpread = highest - lowest;

        if (!BeOptimal) // greedy path
        {
            if (laneSpread > discrepancyWindow)
            {
                if (DebugLogs) Debug.Log($"laneSpread ({laneSpread}) > discrepancyWindow ({discrepancyWindow}) moving to defensive lane");
                int targetIndex = expectedValues.IndexOf(highest);
                if (targetIndex != -1)
                    character.layer = LayerMask.NameToLayer(laneLayerNames[targetIndex]);
            }
            else
            {
                if (DebugLogs) Debug.Log($"laneSpread ({laneSpread}) < discrepancyWindow ({discrepancyWindow}) moving to agressive lane");
                int targetIndex = expectedValues.IndexOf(lowest);
                if (targetIndex != -1)
                    character.layer = LayerMask.NameToLayer(laneLayerNames[targetIndex]);
            }
        }
        else
        {
            // Placeholder for future optimal routing algorithm
        }
    }
}