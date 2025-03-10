using UnityEngine;

public class CpuUtilis: MonoBehaviour
{
    public void SelectOnAttack(int n, ScriptableStats CreatedStats)
    {
        switch(n)
        {
            case 0:
                SpawnMob(CreatedStats);
                break;

            default:
                Debug.LogWarning("Invalid On Attack");
                break;
        }
    }
    public void SpawnMob(ScriptableStats ScrStats)
    {
        //this is stolen from SpawnObjects
        GameObject CreatedObject = Instantiate(this.gameObject, this.transform.position, this.transform.rotation, this.transform);
        CpuLogic ObjectLogic = CreatedObject.GetComponent<CpuLogic>();
        if (ObjectLogic != null) ObjectLogic.ScrStats = ScrStats;

        //rotate apperance if on other side
        if (CreatedObject.transform.childCount > 0 && CreatedObject.transform.GetChild(0).name == "CpuApperance")
        {
            if(CreatedObject.transform.rotation.z > 0)
            {
                CreatedObject.transform.GetChild(0).rotation = new Quaternion(0, 1, 0, 0);
                
            }
            
            //randomize y pos
            float RandomValue = Random.Range(-0.5f, 0.5f);
            CreatedObject.transform.GetChild(0).position = new Vector3
            (
                CreatedObject.transform.GetChild(0).position.x,
                CreatedObject.transform.GetChild(0).position.y + RandomValue,
                CreatedObject.transform.GetChild(0).position.z + RandomValue
            );
        }
    }
}
