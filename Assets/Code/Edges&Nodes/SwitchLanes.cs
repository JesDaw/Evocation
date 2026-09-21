using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// This deals with Ai switching lanes
/// </summary>
public class SwitchLanes : MonoBehaviour
{
    BoxCollider2D myCollider;
    public int currentLayer = 2; // 0 = Top, 1 = Mid, 2 = Bot
    [SerializeField] GameObject[] Groundlevels;
    [SerializeField] GameObject[] ArrowSprites;
    [SerializeField] BoxCollider2D ForkEnterBoxCollider;
    [SerializeField] BoxCollider2D ForkExitBoxCollider;
    [SerializeField] int ForkExitLayernumber; // this is for like when the fork ends and the characters are all walking on the same path
    [SerializeField] bool isAILaneSwitcher;
    [SerializeField] bool DebugLogs = false;

    List<GameObject> charactersInRange = new List<GameObject>();

    static readonly string[] LaneLayerNames = new string[]
    {
        "Allies/TopLane", "Allies/MidLane", "Allies/BotLane",//012
        "Player/TopLane", "Player/MidLane", "Player/BotLane", //345
        "Enemy/TopLane",  "Enemy/MidLane",  "Enemy/BotLane",  //678
         
    };

    void Awake()
    {
        if (ForkEnterBoxCollider == null) myCollider = GetComponent<BoxCollider2D>();
        if (ForkEnterBoxCollider == null) Debug.LogError("All forks need box coliders to know which characters it should effect");
    }

    public float switchCooldown = 1f;
    float lastSwitchTime = 0f;

    void OnTriggerEnter2D(Collider2D collision)
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

    public void ToggleLanes()
    {
        if (DebugLogs) Debug.Log($"ToggleLanes invoked");

        if (Time.time - lastSwitchTime < switchCooldown)
        {
            if (DebugLogs) Debug.Log($"Toggle is still cooling down: {Time.time - lastSwitchTime} left");
            return;
        }

        if (currentLayer >= Groundlevels.Length - 1)
        {
            currentLayer = 0;
        }
        else
        {
            currentLayer++;
        }

        if (DebugLogs) Debug.Log($"Lane switched to: {currentLayer}");

        for (int i = 0; i < ArrowSprites.Length; i++)
        {
            ArrowSprites[i].SetActive(i == currentLayer);
        }

        UpdateAllCharacterLayers();

        if (DebugLogs) Debug.Log($"currentLayer: {currentLayer}, Characters in range: {charactersInRange.Count}");

        lastSwitchTime = Time.time;
    }

    void UpdateAllCharacterLayers()
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
 
     void SetCharacterLayer(GameObject character, int CharacterTeam)
    {
        int offset = 0;
        if (CharacterTeam == 0) offset = 0;      // Allies
        else if (CharacterTeam == 1) offset = 3; // Player
        else if (CharacterTeam == 2) offset = 6; // Enemy
        

        int layerId;
        if ((isAILaneSwitcher && (CharacterTeam == 0 || CharacterTeam == 1)) || (!isAILaneSwitcher && CharacterTeam == 2))
        {
            layerId = ForkExitLayernumber;
        }
        else
        {
            layerId = currentLayer;
        }

        int targetIndex = offset + layerId;

        
        if (isAILaneSwitcher && CharacterTeam <= 1) layerId = ForkExitLayernumber;

        int newLayer = LayerMask.NameToLayer(LaneLayerNames[targetIndex]);
        if (newLayer == -1)
        {
            Debug.LogError($"Layer '{LaneLayerNames[targetIndex]}' not found! Check Project Settings > Tags and Layers.");
            return;
        }

        if (DebugLogs) Debug.Log($"Setting {character.name} to {LaneLayerNames[targetIndex]}");

        character.layer = newLayer;
    }

    void OnTriggerExit2D(Collider2D collision)
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

    int IsCharacter(GameObject obj)
    {
        if (obj.CompareTag("Allies")) return 0;
        if (obj.CompareTag("Player")) return 1;
        if (obj.CompareTag("Enemy")) return 2;
        return -1;
    }
}