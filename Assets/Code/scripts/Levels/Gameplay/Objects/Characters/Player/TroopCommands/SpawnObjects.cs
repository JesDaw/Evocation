using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class SpawnObjects : MonoBehaviour
{
    public bool StopFlag = false;
    public float CoolDown = 1f;
    [SerializeField] GameObject _Object;
    [SerializeField] Transform _CPUContainer;
    [SerializeField] Transform _PlayerContainer;
    [SerializeField] Transform _SpawnLocation;
    [SerializeField] UnityEvent<GameObject> OnSpawn;
    [Header("Entites")]
    [SerializeField] ScriptableStats AttachedStats;
    [SerializeField] bool enemySpawner;
    [SerializeField] bool autoSpawner;
    [SerializeField] FloatVariable _Money;
    [SerializeField] PlayerSwitch playerSwitch;
    [SerializeField] PlayerLivesManager PlayerCountManager;
    [SerializeField] Money _moneyDesplay;

    bool _spawning_is_active = false;

    internal bool SpawningIsActive
    {
        get { return _spawning_is_active; }
        set { _spawning_is_active = value;}
    }

    void Start()
    {
        StartCoroutine(SpawnLoop()); // tis is where the cpu spaner starts working
        _moneyDesplay = FindAnyObjectByType<Money>();
        if (_moneyDesplay == null) Debug.LogError("Spawn objects script can't find Money script to edit the money desplay");
        if(_SpawnLocation == null) Debug.LogError("No place set to spawn objects so spawning wont work");
    }

    IEnumerator SpawnLoop() // cpu spawner
    {
        while (!StopFlag)
        {
            Spawn();
            yield return new WaitForSeconds(CoolDown);
        }
    }

    //same overloaded bullshit spawn script
    //auto spawn
    public void Spawn() // cpu spawner
    {
        if (!_spawning_is_active) return;
        if (!autoSpawner) return;

        GameObject CreatedObject = Instantiate(_Object, _SpawnLocation.transform.position, _SpawnLocation.transform.rotation, _CPUContainer);
        CpuStateManager ObjectLogic = CreatedObject.GetComponent<CpuStateManager>();
        if (ObjectLogic != null) ObjectLogic._ScrStats = AttachedStats;

        // Assign layer
        if (enemySpawner) CreatedObject.layer = 9;
        else CreatedObject.layer = 10;
        foreach (Transform child in CreatedObject.transform) child.gameObject.layer = CreatedObject.layer;

        //rotate apperance if on other side
        if (CreatedObject.transform.childCount > 0 && CreatedObject.transform.GetChild(0).name == "CpuAppearance")
        {
            //randomize y pos
            float RandomValue = Random.Range(-0.5f, 0.5f);
            CreatedObject.transform.GetChild(0).position = new Vector3
            (
                CreatedObject.transform.GetChild(0).position.x,
                CreatedObject.transform.GetChild(0).position.y + RandomValue,
                CreatedObject.transform.GetChild(0).position.z + RandomValue
            );
        }

        OnSpawn.Invoke(CreatedObject);
    }
    public void SpawnFromSpawner(ScriptableStats attachedStat) // I dont think this ever gets called what is the point of this?
    {
        Debug.Log($"spawn from spawner function called");
        if (!_spawning_is_active) return;
        GameObject CreatedObject = Instantiate(_Object, _SpawnLocation.transform.position, _SpawnLocation.transform.rotation, _CPUContainer);
        CpuStateManager ObjectLogic = CreatedObject.GetComponent<CpuStateManager>();
        if (ObjectLogic != null) ObjectLogic._ScrStats = attachedStat;

        // Assign layer
        if (enemySpawner) CreatedObject.layer = 9;
        else CreatedObject.layer = 10;
        foreach (Transform child in CreatedObject.transform) child.gameObject.layer = CreatedObject.layer;

        //rotate apperance if on other side
        if (CreatedObject.transform.childCount > 0 && CreatedObject.transform.GetChild(0).name == "CpuAppearance")
        {
            //randomize y pos
            float RandomValue = Random.Range(-0.5f, 0.5f);
            CreatedObject.transform.GetChild(0).position = new Vector3
            (
                CreatedObject.transform.GetChild(0).position.x,
                CreatedObject.transform.GetChild(0).position.y + RandomValue,
                CreatedObject.transform.GetChild(0).position.z + RandomValue
            );
        }

        OnSpawn.Invoke(CreatedObject);
    }

    // when player manually spawns
    public void Spawn(ScriptableStats ScrStats)// this is what the player uses
    {
        if (!_spawning_is_active)
        {
            Debug.Log("Character spawns are dissabled");
            return;
        }
        if (_Money._Value < ScrStats._spawnCost)
        {
            Debug.Log("Not enough money!");
            return;
        }

        _Money._Value -= ScrStats._spawnCost;
        _moneyDesplay.UpdateMoneyDesplay();
        //Debug.Log("money updated:" + _Money._Value);


        GameObject CreatedObject = Instantiate(_Object, _SpawnLocation.transform.position, _SpawnLocation.transform.rotation, _CPUContainer);
        CpuStateManager ObjectLogic = CreatedObject.GetComponent<CpuStateManager>();
        if (ObjectLogic != null) ObjectLogic._ScrStats = ScrStats;
        
        // Assign layer
        if (enemySpawner) CreatedObject.layer = 9;
        else CreatedObject.layer = 10;        
        foreach (Transform child in CreatedObject.transform) child.gameObject.layer = CreatedObject.layer;

        // Rotate appearance if on other side
        if (CreatedObject.transform.childCount > 0 && CreatedObject.transform.GetChild(0).name == "CpuApperance")
        {
            if (CreatedObject.transform.rotation.z > 0)
            {
                CreatedObject.transform.GetChild(0).rotation = new Quaternion(0, 1, 0, 0);
            }

            // Randomize y position
            float RandomValue = Random.Range(-0.5f, 0.5f);
            CreatedObject.transform.GetChild(0).position = new Vector3
            (
                CreatedObject.transform.GetChild(0).position.x,
                CreatedObject.transform.GetChild(0).position.y + RandomValue,
                CreatedObject.transform.GetChild(0).position.z + RandomValue
            );

        }
        OnSpawn.Invoke(CreatedObject);
    }
    public void SpawnPlayer(GameObject player) // player also uses this
    {
        if (!_spawning_is_active)
        {
            Debug.Log("Character spawns are dissabled");
            return;
        }
        if (!PlayerCountManager.canSpawnMore) return;
        int cost = player.GetComponent<Stats>()._spawnCost;
        if (_Money._Value > cost) _Money._Value -= cost;
        else 
        {
            Debug.Log("Not enough money!");
            return;
        }

        GameObject CreatedObject = Instantiate(player, _SpawnLocation.transform.position, _SpawnLocation.transform.rotation, _PlayerContainer);
        
        // Subscribe to the player's death event using UltEvents
        Stats playerStats = CreatedObject.GetComponent<Stats>();
        if (playerStats != null)
        {
            // UltEvents uses AddPersistentCall or you can use the delegate directly
            playerStats.OnDeath.DynamicCalls += () => PlayerCountManager.LooseLife(CreatedObject);
        }
        else
        {
            Debug.LogError("Player prefab does not have a Stats component!");
        }
        
        playerSwitch.AddPlayer(CreatedObject);
        PlayerCountManager.GainLife();
    }
}
