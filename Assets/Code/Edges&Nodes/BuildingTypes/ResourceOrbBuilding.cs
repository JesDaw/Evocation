using UnityEngine;


public class ResourceOrbBuilding : MonoBehaviour
{
    [SerializeField] ResourceSpawner[] resourceSpawners;
    [SerializeField] ResourceChange[] resourceChange;
    
 
    public void ApplyEffect()
    {
        //building effects
        foreach (var resource in resourceSpawners)
        {
            foreach(var change in resourceChange)
            {
                if (resource.resourceType == change.ResourceTypeToChangeInto)
                {
                    //effect
                    resource.ChangeData(change);
                }
            }
            
        }
    }

   public void RevertEffect()
    {
       foreach (var resource in resourceSpawners)
        {
            resource.RevertData();
        }
    }
}

[System.Serializable]
public class ResourceChange
{
    public ResourceType ResourceTypeToApplyTo;
    public ResourceType ResourceTypeToChangeInto;
    public float SpawnRateMultiplier = 1;
    public float ValueMultiplier = 1;


}