using UnityEngine;
using System.Collections;

public class AIMoneyManager : MonoBehaviour
{
    [Header("Resources")]
    [HideInInspector] public int genPerSec = 1;
    [Header("Settings")]
    [SerializeField] float maxMoney = 9999f;
    bool isActive = false;
    public float CurrentMoney { get; private set; }

    public bool MoneyIsActive => isActive;
    [SerializeField] bool DebugLogs = false;
    public static AIMoneyManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        CurrentMoney = 0f;
    }

    void Start()
    {   
        StartCoroutine(MoneyGeneration());
    }

    IEnumerator MoneyGeneration()
    {
        while (true)
        {
            if (!isActive || CurrentMoney >= maxMoney)
            {
                yield return null;
                continue;
            }

            CurrentMoney += 1;
            if (DebugLogs) Debug.Log("AI money amount: " + CurrentMoney);
            
            yield return new WaitForSeconds(1 / genPerSec);
        }
    }

    public void GiveMoney(float amount)
    {
        CurrentMoney += amount;
    }

    public void SpendMoney(float amount)
    {
        CurrentMoney -= amount;
    }

    public void ActivateMoney()
    {
        isActive = true;
        //Debug.Log("AI Money generation activated");
    }

    public void DeactivateMoney()
    {
        isActive = false;
        //Debug.Log("AI Money generation deactivated");
    }

    public void IncreaseMoneyGen()
    {
        genPerSec *= 2;
        if(DebugLogs) Debug.Log($"AI Money generation increased to {genPerSec}/sec");
    }

    public float GetMoney()
    {
        return CurrentMoney;
    }
}