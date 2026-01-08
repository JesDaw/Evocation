using UnityEngine;
using System.Collections;

/// <summary>
/// Money manager for AI - standalone implementation without UI
/// Uses the same generation logic as player Money but without display
/// </summary>
public class AIMoneyManager : MonoBehaviour
{
    [Header("Resources")]
    [SerializeField] public FloatVariable moneyAmount;
    [SerializeField] public FloatVariable genPerSec;
    
    [Header("Settings")]
    [SerializeField] float maxMoney = 9999f;
    [SerializeField] bool autoStart = true;

    bool isActive = false;
    public float CurrentMoney { get; private set; }

    public bool MoneyIsActive => isActive;

    void Awake()
    {
        if (moneyAmount != null)
            CurrentMoney = moneyAmount._Value;
    }

    void Start()
    {
        if (autoStart)
            ActivateMoney();
        
        StartCoroutine(MoneyGeneration());
    }

    IEnumerator MoneyGeneration()
    {
        while (true)
        {
            if (!isActive || moneyAmount == null || moneyAmount._Value >= maxMoney)
            {
                yield return null;
                continue;
            }

            moneyAmount._Value += 1;
            CurrentMoney = moneyAmount._Value;
            
            yield return new WaitForSeconds(1 / genPerSec._Value);
        }
    }

    public void SpendMoney(float amount)
    {
        if (moneyAmount != null)
        {
            moneyAmount._Value -= amount;
            CurrentMoney = moneyAmount._Value;
        }
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

    public void ResetMoney()
    {
        if (moneyAmount != null)
        {
            moneyAmount.Reset();
            CurrentMoney = moneyAmount._Value;
        }
    }

    public void IncreaseMoneyGen()
    {
        if (genPerSec != null)
        {
            genPerSec._Value *= 2;
            Debug.Log($"AI Money generation increased to {genPerSec._Value}/sec");
        }
    }

    /// <summary>
    /// Get current money value
    /// </summary>
    public float GetMoney()
    {
        return moneyAmount != null ? moneyAmount._Value : 0f;
    }
}