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
        GameObject CreatedObject = Instantiate(_Object, this.transform.position, this.transform.rotation, _Container);
        CpuLogic ObjectLogic = CreatedObject.GetComponent<CpuLogic>();
        if (ObjectLogic != null) ObjectLogic.ScrStats = AttachedStats;

        yield return new WaitForSeconds(CoolDown);

        if (!StopFlag) StartCoroutine(SpawnLoop());
    }
}
