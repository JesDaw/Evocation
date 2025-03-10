using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using System.Collections.Generic;

public class CpuController : MonoBehaviour
{
    public UnityEvent<ScriptableStats> OnSpawn1, OnSpawn2;
    [SerializeField] Money _Money;
    public void Spawn1(InputAction.CallbackContext context)
    {
        if (!context.started) return;

        _Money.spendMoney(10);

        OnSpawn1?.Invoke(null);
    }
    public void Spawn2(InputAction.CallbackContext context)
    {
        if (!context.started) return;

        _Money.spendMoney(20);

        OnSpawn2?.Invoke(null);
    }
}