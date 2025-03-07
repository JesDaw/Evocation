using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu]
public class Event : ScriptableObject
{
    List<EventListiner> listiners = new List<EventListiner>();

    public void Raise()
    {
        for (int i = listiners.Count -1; i >= 0; i--)
        {
            if (listiners[i] == null)
            {
                listiners.RemoveAt(i);
                continue;
            }
            listiners[i].OnEventRaised();
            Debug.Log("Event signal sent to" + listiners[i].gameObject + "object");
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
