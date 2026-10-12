using UnityEngine;
using System.Collections;

public class RootBuilding : MapStructure
{
    bool active = false;
    [SerializeField] float loopInterval = 6.0f;
    [SerializeField][Range(0f, 1f)] float activationChance = 0.25f;
    [SerializeField] StaticStatusEffect effect;
    public override void ApplyEffect()
    {
        //building effects
        active = true;
        StartCoroutine(RootTarget());
    }

    public override void RevertEffect()
    {
        active = false;
        StopCoroutine(RootTarget());
    }

    IEnumerator RootTarget()
    {
        while (active)
        {
            foreach (MapZone zone in UnitTracker.Instance.zones)
            {
                foreach (GameObject unit in zone.EnemyUnits)
                {
                    if (Random.Range(0f, 1f) <= activationChance)
                    {
                        unit.GetComponent<StatusEffectManager>().AddEffect(effect);
                    }
                }
            }
            yield return new WaitForSeconds(loopInterval);
        }
    }
}
