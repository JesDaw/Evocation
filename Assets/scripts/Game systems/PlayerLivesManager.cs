using UnityEngine;
using UnityEngine.Events;


//script need a corsponding player deth tracker
public class PlayerLivesManager : MonoBehaviour
{
   [SerializeField]  IntVeriable LifeCount;
    public UnityEvent PlayerDeath;
    public UnityEvent PlayerSpawn;
    [SerializeField] IntVeriable MaxLives;
    bool canSpawnMore;

    public void GainLife()
    {
        LifeCount._Value++;
        if (LifeCount._Value == MaxLives._Value)
        {
            canSpawnMore =false;
        }
    }
    public void LooseLife()
    {
        LifeCount._Value--;
        canSpawnMore = true;
        if (LifeCount._Value == 0)
        {
            //invoke game over event
        }
    }
}
