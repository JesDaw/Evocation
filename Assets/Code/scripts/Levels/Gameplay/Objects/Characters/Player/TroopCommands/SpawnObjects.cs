using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class SpawnObjects : MonoBehaviour
{
    public bool StopFlag = false;
    public float CoolDown = 1f;
    [SerializeField] GameObject _Object;
    [SerializeField] Transform _Container;
    [SerializeField] Transform _PlayerContainer;
    [SerializeField] UnityEvent<GameObject> OnSpawn;
    [Header("Entites")]
    [SerializeField] ScriptableStats AttachedStats;
    [SerializeField] bool enemySpawner;
    [SerializeField] bool autoSpawner;
    [SerializeField] FloatVariable _Money;
    [SerializeField] PlayerSwitch playerSwitch;
    [SerializeField] PlayerLivesManager PlayerCountManager;
    [SerializeField] Money _moneyDesplay;

    void Start()
    {
        StartCoroutine(SpawnLoop());
        _moneyDesplay = FindAnyObjectByType<Money>();
        if (_moneyDesplay == null) Debug.LogError("Spawn objects script can't find Money script to edit the money desplay");
    }

    IEnumerator SpawnLoop()
    {
        if(StopFlag) yield break;
        Spawn();
        yield return new WaitForSeconds(CoolDown);
        StartCoroutine(SpawnLoop());
    }
    //same overloaded bullshit spawn script
    //auto spawn
    public void Spawn()
    {
        if (!autoSpawner) return;

        GameObject CreatedObject = Instantiate(_Object, this.transform.position, this.transform.rotation, _Container);
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

        if (enemySpawner) ObjectLogic._Stats._Enemy = true;
        OnSpawn.Invoke(CreatedObject);
    }

    // when player manually spawns
    public void Spawn(ScriptableStats ScrStats)
    {
        if (_Money._Value < ScrStats._spawnCost)
        {
            Debug.Log("Not enough money!");
            return;    
        }

        _Money._Value -= ScrStats._spawnCost;
        _moneyDesplay.UpdateMoneyDesplay();
        Debug.Log("money updated:" + _Money._Value);


        GameObject CreatedObject = Instantiate(_Object, this.transform.position, this.transform.rotation, _Container);
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
    }
    public void SpawnPlayer(GameObject player)
    {
        if (!PlayerCountManager.canSpawnMore) return;
        int cost = player.GetComponent<Stats>()._spawnCost;
        if (_Money._Value > cost) _Money._Value -= cost;
        else 
        {
            Debug.Log("Not enough money!");
            return;
        }

        GameObject CreatedObject = Instantiate(player, this.transform.position, this.transform.rotation, _PlayerContainer);
        playerSwitch.AddPlayer(CreatedObject);
        PlayerCountManager.GainLife();
    }
}
