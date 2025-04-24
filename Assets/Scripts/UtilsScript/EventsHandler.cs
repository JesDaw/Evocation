using UnityEngine;

public class EventsHandler : MonoBehaviour
{
    public static EventsHandler Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    private void Setup()
    {
        //this is here because for some reason you can't put multiple parametsers in unity events
        //so we'll just add them using scripts
    }

    public static void BuffTeam(string _holderName, StatusEffect _effect)
    {
        GameObject _holderItem = GameObject.Find(_holderName);
        StatsHolder _holderStats = _holderItem.GetComponent<StatsHolder>();

        for (int I = 0; I < _holderStats.Count; I++)
        {
            _holderStats[I].AddStatusEffect(_effect);
        }
    }
}
