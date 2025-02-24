using UnityEngine;


//script need a corsponding player deth tracker
public class PlayerLivesManager : MonoBehaviour
{
   [SerializeField]  IntVeriable LifeCount;
    void Start()
    {
        
    }

    // Update is called once per frame
    void GaimLife()
    {
        LifeCount._Value++;
    }
    void LooseLife()
    {
        LifeCount._Value--;
    }
}
