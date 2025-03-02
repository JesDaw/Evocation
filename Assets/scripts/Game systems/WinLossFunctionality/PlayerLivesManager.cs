using UnityEngine;
using UnityEngine.Events;
using TMPro;


//script need a corsponding player deth tracker
public class PlayerLivesManager : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI LifeText;
   [SerializeField]  IntVeriable LifeCount;
    // public UnityEvent PlayerDeath;
    // public UnityEvent PlayerSpawn;
    [SerializeField] int MaxLives;
    [SerializeField] UnityEvent _loose_game;
    bool canSpawnMore;

    void Update()
    {
        LifeText.text = LifeCount._Value.ToString("0");
    }
    public void GainLife()
    {
        if (canSpawnMore)
        {
            LifeCount._Value++;
         if (LifeCount._Value == MaxLives)
         {
             canSpawnMore =false;
         }
        }
    }
    public void LooseLife()
    {
        LifeCount._Value--;
        canSpawnMore = true;
        if (LifeCount._Value == 0)
        {
            _loose_game.Invoke();
        }
    }
}
