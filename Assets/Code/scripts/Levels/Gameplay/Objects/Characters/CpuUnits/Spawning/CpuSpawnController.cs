using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CpuSpawnController : MonoBehaviour
{
    public float distanceToBase;
    public int money;
    public enum State
    {
        NormalEnemy,
        RapidSpawn
    }
    public List<CpuAction> actionPool;
    public List<CpuAction> totalAction;

    void Start()
    {
        for (int I = 0; I < totalAction.Count; ++I)
        {
            totalAction[I].AssignController(this);
        }
    }

    void PerformAction()
    {
        NewActionPool();

        int index = UnityEngine.Random.Range(0, actionPool.Count);
        CpuAction chosen = actionPool[index];
        chosen.UseAction();
    }

    void NewActionPool()
    {
        actionPool.Clear();

        for(int I = 0; I < totalAction.Count; ++I)
        {
            if (totalAction[I].EvalBasedOnCondition())
                actionPool.Append(totalAction[I]); 
        }
    }
}