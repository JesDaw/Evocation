using UnityEngine;
using System.Collections.Generic;

public class StatsHolder : MonoBehaviour
{
    //dunno if it's the right choice, but this is just some code
    //so I don't have to access the underlying containerStats
    [SerializeField] private List<Stats> ContainerStats = new();

    public Stats this[int index]
    {
        get => ContainerStats[index];
        set => ContainerStats[index] = value;
    }
    public int Count => ContainerStats.Count;
    public void Add(Stats stat) => ContainerStats.Add(stat);
    public void Add(GameObject statsObject) => ContainerStats.Add(statsObject.GetComponent<Stats>());
}
