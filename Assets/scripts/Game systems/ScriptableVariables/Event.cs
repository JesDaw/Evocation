using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu]
public class Event : ScriptableObject
{
    List<EventListiner> listiners = new List<EventListiner>();

    public void Raise()
    {
        Debug.Log("here 1.5");
        Debug.Log(listiners.Count);
        Debug.Log(listiners.Count - 1);
        for (int i = listiners.Count -1; i >= 0; i--)
        {
            listiners[i].OnEventRaised();
            Debug.Log("here 2");
        }
    }

    public void RegisterListener(EventListiner listiner)
    {
        listiners.Add(listiner);
    }
        public void DeregisterListener(EventListiner listiner)
    {
        listiners.Remove(listiner);
    }


}
