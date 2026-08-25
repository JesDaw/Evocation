using UnityEngine;
using System.Collections.Generic;

public class SwitchLanes : MonoBehaviour
{
    BoxCollider2D myCollider;
    [SerializeField] int currentLayer = 2; // 0 = Top, 1 = Mid, 2 = Bot
    [SerializeField] GameObject[] Groundlevels;
    [SerializeField] GameObject[] ArrowSprites;
    [SerializeField] BoxCollider2D BoxCollider;
    [SerializeField] bool DebugLogs = false;

    // List to track characters in the collider
    private List<GameObject> charactersInRange = new List<GameObject>();

    // Lane layer names now live in UnitTracker (UnitTracker.LaneLayerNames) so this
    // script and UnitTracker's lane cache can never drift out of sync on naming/casing.
    // Order: Allies/Top, Allies/Mid, Allies/Bot, Enemy/Top, Enemy/Mid, Enemy/Bot,
    //        Player/Top, Player/Mid, Player/Bot  (offsets 0, 3, 6 below)

    void Awake()
    {
        if (BoxCollider == null) myCollider = GetComponent<BoxCollider2D>();
        if (BoxCollider == null) Debug.LogError("All forks need box coliders to know which characters it should effect");
    }

    public float switchCooldown = 1f;
    float lastSwitchTime = 0f;

    public void ToggleLanes()
    {
        if (DebugLogs) Debug.Log($"ToggleLanes invoked");

        if (Time.time - lastSwitchTime < switchCooldown)
        {
            if (DebugLogs) Debug.Log($"Toggle is still cooling down: {Time.time - lastSwitchTime} left");
            return;
        }

        // Cycle through lanes
        if (currentLayer >= Groundlevels.Length - 1)
        {
            currentLayer = 0;
        }
        else
        {
            currentLayer++;
        }

        if (DebugLogs) Debug.Log($"Lane switched to: {currentLayer}");

        // Update arrow sprites
        for (int i = 0; i < ArrowSprites.Length; i++)
        {
            ArrowSprites[i].SetActive(i == currentLayer);
        }

        // Update all characters currently in the collider
        UpdateAllCharacterLayers();

        if (DebugLogs) Debug.Log($"currentLayer: {currentLayer}, Characters in range: {charactersInRange.Count}");

        lastSwitchTime = Time.time;
    }

    private void UpdateAllCharacterLayers()
    {
        for (int i = charactersInRange.Count - 1; i >= 0; i--)
        {
            GameObject character = charactersInRange[i];
            if (character == null)
            {
                charactersInRange.RemoveAt(i);
                continue;
            }

            SetCharacterLayer(character, IsCharacter(character));
        }
    }

    private int IsCharacter(GameObject obj)
    {
        if (obj.CompareTag("Allies")) return 0;
        if (obj.CompareTag("Player")) return 1;
        if (obj.CompareTag("Enemy")) return 2;
        return -1;
    }

    private void SetCharacterLayer(GameObject character, int tagType)
    {
        int offset = 0;
        if (tagType == 0) offset = 0;      // Allies
        else if (tagType == 2) offset = 3; // Enemy
        else if (tagType == 1) offset = 6; // Player

        int targetIndex = offset + currentLayer;

        if (UnitTracker.Instance == null)
        {
            Debug.LogError("SwitchLanes: UnitTracker.Instance is null, can't update lane cache or layer.");
            return;
        }

        if (DebugLogs) Debug.Log($"Setting {character.name} to {UnitTracker.LaneLayerNames[targetIndex]}");

        // Routes through UnitTracker so its per-lane cache stays correct, instead of
        // setting character.layer directly here.
        UnitTracker.Instance.SetUnitLane(character, targetIndex);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (IsCharacter(collision.gameObject) >= 0)
        {
            if (DebugLogs) Debug.Log($"{collision.gameObject.name} entered lane switch. Previous layer: {LayerMask.LayerToName(collision.gameObject.layer)}");

            if (!charactersInRange.Contains(collision.gameObject))
            {
                if (DebugLogs) Debug.Log($"Adding {collision.gameObject.name} to characters in range list");
                charactersInRange.Add(collision.gameObject);
            }

            SetCharacterLayer(collision.gameObject, IsCharacter(collision.gameObject));

            if (DebugLogs) Debug.Log($"{collision.gameObject.name} layer is now: {LayerMask.LayerToName(collision.gameObject.layer)}");
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (IsCharacter(collision.gameObject) >= 0)
        {
            if (DebugLogs) Debug.Log($"{collision.gameObject.name} left lane switch");

            if (charactersInRange.Contains(collision.gameObject))
            {
                charactersInRange.Remove(collision.gameObject);
                if (DebugLogs) Debug.Log($"Removed {collision.gameObject.name} from list. Characters remaining: {charactersInRange.Count}");
            }
        }
    }
}