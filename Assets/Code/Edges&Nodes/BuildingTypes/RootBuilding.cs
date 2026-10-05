using UnityEngine;

public class RootBuilding : MapStructure
{
    [SerializeField] StaticStatusEffect effect;
    public override void ApplyEffect()
    {
        //building effects
        foreach(MapZone zone in UnitTracker.Instance.zones)
        {
            foreach(GameObject unit in zone.EnemyUnits)
            {
                unit.GetComponent<StatusEffectManager>().AddEffect(effect);
            }
        }
    }

    public override void RevertEffect()
    {
        
    }
}
