using UnityEngine;
using TMPro;
using UnityEngine.Events;
using System.Collections;

public class Money : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI moneyText;
    //[SerializeField] AIMoneyManager aIMoneyManager;
    [SerializeField] float InitialMoneyGainPerSec = 1;
    float MoneyGainPerSec = 1;
    bool _money_is_active = false;
    [SerializeField] [Range(0,1)]float StartingMoneyPercentOfMax = 0;
    [HideInInspector] public float CurrentMoney = 0;
    [HideInInspector] public int CurrentMaxMoneyIndex = 0;
    [SerializeField] float[] MaxMoney = {200, 400, 600, 800, 1000};
    [SerializeField] float[] CostToUpgradeMaxMoneyPercent = {.75f, .75f, .75f, .75f, .75f};
    public UnityEvent MoneyUpdated;
    [SerializeField] bool DebugLogs = false;
    
    public bool MoneyIsActive
    {
        get { return _money_is_active; }
    }
    public static Money Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (moneyText == null)
        {
            GameObject moneyTextObj = GameObject.Find("MoneyText");
            if (moneyTextObj == null) Debug.LogError("MoneyManager could not find the MoneyText game object");
            moneyText = moneyTextObj.GetComponent<TextMeshProUGUI>();
        }
        if(StartingMoneyPercentOfMax > 1) StartingMoneyPercentOfMax = 1;
        if(StartingMoneyPercentOfMax < 0) StartingMoneyPercentOfMax = 0;
        CurrentMoney = StartingMoneyPercentOfMax*MaxMoney[0];
        MoneyGainPerSec = InitialMoneyGainPerSec;
    }

    void Start()
    {
        StartCoroutine(moneyCount());
        UpdateMoneyDesplay();
    }

    IEnumerator moneyCount()
    {
        while(true)
        {
            if (!_money_is_active || CurrentMoney >= MaxMoney[CurrentMaxMoneyIndex])
            {
                yield return null;
                continue;
            }
            CurrentMoney += 1;
            MoneyUpdate();
            yield return new WaitForSeconds(1/MoneyGainPerSec);
        }
    }

    void MoneyUpdate()
    {
        MoneyUpdated?.Invoke();
        UpdateMoneyDesplay();
    }

    public void UpdateMoneyDesplay()
    {
        moneyText.text = $"{CurrentMoney.ToString("0")}/{MaxMoney[CurrentMaxMoneyIndex]}";
        //Debug.Log("UpdateMoneyDesplay updated");
    }

    public void AddMoney(float amount)
    {
        CurrentMoney += amount;
        if (CurrentMoney > MaxMoney[CurrentMaxMoneyIndex]) CurrentMoney = MaxMoney[CurrentMaxMoneyIndex];
        MoneyUpdate();
    }

    public void spendMoney(float amount)
    {
        CurrentMoney -= amount;
        MoneyUpdate();
    }

    public void UpgradeMaxMoney()
    {
        if(MaxMoney.Length - 1 > CurrentMaxMoneyIndex && CurrentMoney >= MaxMoney[CurrentMaxMoneyIndex] * CostToUpgradeMaxMoneyPercent[CurrentMaxMoneyIndex])
        {
            spendMoney(MaxMoney[CurrentMaxMoneyIndex] * CostToUpgradeMaxMoneyPercent[CurrentMaxMoneyIndex]);
            CurrentMaxMoneyIndex += 1;
            UpdateMoneyGen();
            //effects;
        }
    }

    public void UpdateMoneyGen() 
    { 
        MoneyGainPerSec = InitialMoneyGainPerSec * (MaxMoney[CurrentMaxMoneyIndex]/MaxMoney[0]);
        if (DebugLogs) Debug.Log($"money gen per sec = {InitialMoneyGainPerSec} * {MaxMoney[CurrentMaxMoneyIndex]} / {MaxMoney[0]} = {MoneyGainPerSec}");
    }
    
    public void DeactivateMoney() 
    { 
        _money_is_active = false; 
        //aIMoneyManager.DeactivateMoney();
    }
    public void ActivateMoney() 
    { 
        _money_is_active = true; 
        //aIMoneyManager.ActivateMoney();
    }
    public void ResetMoney() => CurrentMoney = 0; 
    
    public void MoneybuildingGen()
    {
        MoneyGainPerSec += 10;
    }
    
}