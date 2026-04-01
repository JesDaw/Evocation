using UnityEngine;
using System.Collections.Generic;

public class SwitchLanes : MonoBehaviour
{
    BoxCollider2D myCollider;
    [SerializeField] int currentLayer = 2; // 0 = Top, 1 = Mid, 2 = Bot
    [SerializeField] GameObject[] Groundlevels;
    [SerializeField] GameObject[] ArrowSprites;
    [SerializeField] BoxCollider2D BoxCollider;
    [SerializeField]bool DebugLogs = false;

    // List to track characters in the collider
    private List<GameObject> charactersInRange = new List<GameObject>();

    // Layer names for each lane (CHARACTER layers, not ground)
    private string[] laneLayerNames = new string[] 
    {
        "Character/TopLane",   // currentLayer 0
        "Character/MidLane",   // currentLayer 1
        "Character/BotLane"    // currentLayer 2
    };

    void Awake()
    {
        if (BoxCollider == null) myCollider = GetComponent<BoxCollider2D>();
        if (BoxCollider == null) Debug.LogError("All forks need box coliders to know which characters it should effect");
    }

    public float switchCooldown = 1f;
    float lastSwitchTime = 0f;

    public void ToggleLanes()
    {
        if(DebugLogs) Debug.Log($"ToggleLanes invoked");

        if (Time.time - lastSwitchTime < switchCooldown) 
        {
            if(DebugLogs) Debug.Log($"Toggle is still cooling down: {Time.time - lastSwitchTime} left");
            return;
        }

        // Cycle through lanes
        if(currentLayer >= Groundlevels.Length - 1)
        {
            currentLayer = 0;
        }
        else 
        {
            currentLayer++;
        }

        if(DebugLogs) Debug.Log($"Lane switched to: {currentLayer} ({laneLayerNames[currentLayer]})");

        // Update arrow sprites
        for (int i = 0; i < ArrowSprites.Length; i++)
        {
            ArrowSprites[i].SetActive(i == currentLayer);
        }

        // Update all characters currently in the collider
        UpdateAllCharacterLayers();

        if(DebugLogs) Debug.Log($"currentLayer: {currentLayer}, Characters in range: {charactersInRange.Count}");

        lastSwitchTime = Time.time;
    }

    private void UpdateAllCharacterLayers()
    {
        // Update layer for all characters in range
        for (int i = charactersInRange.Count - 1; i >= 0; i--)
        {
            GameObject character = charactersInRange[i];
            
            // Clean up null references (destroyed objects)
            if (character == null)
            {
                charactersInRange.RemoveAt(i);
                continue;
            }
            
            SetCharacterLayer(character);
        }
    }

    private bool IsCharacter(GameObject obj)
    {
        return obj.CompareTag("Allies") || obj.CompareTag("Enemy") || obj.CompareTag("Player");
    }

    private void SetCharacterLayer(GameObject character)
    {
        string newLayerName = laneLayerNames[currentLayer];
        int newLayer = LayerMask.NameToLayer(newLayerName);

        if (newLayer != -1)
        {
            if(DebugLogs) Debug.Log($"Setting {character.name} to layer {newLayerName} (layer #{newLayer})");
            character.layer = newLayer;
        }
        else
        {
            Debug.LogError($"Layer '{newLayerName}' not found! Make sure it exists in your project settings.");
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (IsCharacter(collision.gameObject))
        {
            if(DebugLogs) Debug.Log($"{collision.gameObject.name} entered lane switch. Previous layer: {LayerMask.LayerToName(collision.gameObject.layer)}");

            // Add to list if not already present
            if (!charactersInRange.Contains(collision.gameObject))
            {
                if(DebugLogs) Debug.Log($"Adding {collision.gameObject.name} to characters in range list");
                charactersInRange.Add(collision.gameObject);
            }

            // Set the character to the current lane
            SetCharacterLayer(collision.gameObject);

            if(DebugLogs) Debug.Log($"{collision.gameObject.name} layer is now: {LayerMask.LayerToName(collision.gameObject.layer)}");
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (IsCharacter(collision.gameObject))
        {
            if(DebugLogs) Debug.Log($"{collision.gameObject.name} left lane switch");

            // Remove from list
            if (charactersInRange.Contains(collision.gameObject))
            {
                charactersInRange.Remove(collision.gameObject);
                if(DebugLogs) Debug.Log($"Removed {collision.gameObject.name} from list. Characters remaining: {charactersInRange.Count}");
            }
        }
    }
}