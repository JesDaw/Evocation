using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class SpawnObjects : MonoBehaviour
{
    public bool StopFlag = false;
    public float CoolDown = 1f;
    [SerializeField] GameObject _Object;
    [SerializeField] Transform _Container;
    [SerializeField] UnityEvent OnSpawn;
    [Header("Entites")]
    [SerializeField] ScriptableStats AttachedStats;

    void Start()
    {
        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        if(StopFlag) yield break;
        Spawn();
        yield return new WaitForSeconds(CoolDown);
        StartCoroutine(SpawnLoop());
    }
    public void UpdateAttached(ScriptableStats ScrStats)
    {
        AttachedStats = ScrStats;
    }
    public void Spawn()
    {
        GameObject CreatedObject = Instantiate(_Object, this.transform.position, this.transform.rotation, _Container);
        CpuLogic ObjectLogic = CreatedObject.GetComponent<CpuLogic>();
        if (ObjectLogic != null) ObjectLogic.ScrStats = AttachedStats;

        //rotate apperance if on other side
        if (CreatedObject.transform.childCount > 0 && CreatedObject.transform.GetChild(0).name == "CpuApperance")
        {
            if(CreatedObject.transform.rotation.z > 0)
            {
                CreatedObject.transform.GetChild(0).rotation = new Quaternion(0, 1, 0, 0);
                
            }
            
            //randomize y pos
            CreatedObject.transform.GetChild(0).position = new Vector3
            (
                CreatedObject.transform.GetChild(0).position.x,
                CreatedObject.transform.GetChild(0).position.y + Random.Range(-0.5f, 0.5f),
                CreatedObject.transform.GetChild(0).position.z
            );
        }
    }
}
