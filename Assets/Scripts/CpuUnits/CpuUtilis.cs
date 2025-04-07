using UnityEngine;

//this script handels all external/ unique actions an enemy could do
public class CpuUtilis: MonoBehaviour
{
    [SerializeField] GameObject ProjectilesOverlay;
    public void SelectOnAttack(int n, ScriptableStats ScrStats, GameObject EnemyObject)
    {
        switch(n)
        {
            case 0:
                SpawnMob(ScrStats.ExtraStats);
                break;
            case 1:
                ShootProjectiles(ScrStats._Projectiles[0], EnemyObject.transform);
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

    public void ShootProjectiles(ScrProjectiles _Projectile, Transform _Enemy)
    {
        Debug.Log("" + _Projectile.ToString());
        GameObject CreatedProjectile = Instantiate(ProjectilesOverlay, transform.position, transform.rotation, null);
        CreatedProjectile.GetComponent<ProjectileScript>().UpdateProjectile(transform.position, _Enemy.gameObject, _Projectile);
    }
}
