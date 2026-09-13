using UnityEngine;
using TMPro;
using UnityEngine.Events;
using System.Collections;

public class Money : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI moneyText;
    [SerializeField] float InitialMoneyGainPerSec = 1;
    float MoneyGainPerSec = 1;
    bool _money_is_active = false;
    [SerializeField] [Range(0,1)]float StartingMoneyPercentOfMax = 0;
    [HideInInspector] public int CurrentMoney = 0;
    [HideInInspector] public int CurrentMaxMoneyIndex = 0;
    [SerializeField] int[] MaxMoney = {200, 400, 600, 800, 1000};
    [SerializeField] float[] CostToUpgradeMaxMoneyPercent = {.75f, .7428571429f, .76f, .861f, .75f};
    [SerializeField] TextMeshProUGUI PriceToUbgradeUGUI;
    public UnityEvent MoneyUpdated;
    [SerializeField] bool IsAI;
    [SerializeField] bool DebugLogs = false;
    
    public bool MoneyIsActive
    {
        get { return _money_is_active; }
    }
    public static Money Instance { get; private set; }
    public static Money AIInstance { get; private set; }

    void Awake()
    {
        if (IsAI)
        {
            if (AIInstance != null && AIInstance != this)
            {
                Destroy(gameObject);
                return;
            }

            AIInstance = this;
        }
        else
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
        }
        
        if(StartingMoneyPercentOfMax > 1) StartingMoneyPercentOfMax = 1;
        if(StartingMoneyPercentOfMax < 0) StartingMoneyPercentOfMax = 0;
        CurrentMoney = Mathf.FloorToInt(StartingMoneyPercentOfMax * MaxMoney[0]);
        MoneyGainPerSec = InitialMoneyGainPerSec;
    }

    void Start()
    {
        StartCoroutine(moneyCount());
        if (!IsAI) UpdateMoneyDesplay();
        if (!IsAI) PriceToUbgradeUGUI.text = (MaxMoney[CurrentMaxMoneyIndex]*CostToUpgradeMaxMoneyPercent[CurrentMaxMoneyIndex]).ToString("0");
        if (DebugLogs) Debug.Log($"desplaying {(MaxMoney[CurrentMaxMoneyIndex]*CostToUpgradeMaxMoneyPercent[CurrentMaxMoneyIndex]).ToString("0")} as upgrade price");

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
        if (!IsAI) UpdateMoneyDesplay();
    }

    public void UpdateMoneyDesplay()
    {
        moneyText.text = $"{CurrentMoney.ToString("0")}/{MaxMoney[CurrentMaxMoneyIndex]}";
    }

    public void AddMoney(int amount)
    {
        CurrentMoney += amount;
        if (CurrentMoney > MaxMoney[CurrentMaxMoneyIndex]) CurrentMoney = MaxMoney[CurrentMaxMoneyIndex];
        MoneyUpdate();
    }

    public void spendMoney(int amount)
    {
        CurrentMoney -= amount;
        MoneyUpdate();
    }

    public void UpgradeMaxMoney()
    {
        if(MaxMoney.Length - 1 > CurrentMaxMoneyIndex && CurrentMoney >= MaxMoney[CurrentMaxMoneyIndex] * CostToUpgradeMaxMoneyPercent[CurrentMaxMoneyIndex])
        {
            spendMoney(Mathf.FloorToInt(MaxMoney[CurrentMaxMoneyIndex] * CostToUpgradeMaxMoneyPercent[CurrentMaxMoneyIndex]));
            CurrentMaxMoneyIndex += 1;
            if (!IsAI) PriceToUbgradeUGUI.text = (MaxMoney[CurrentMaxMoneyIndex]*CostToUpgradeMaxMoneyPercent[CurrentMaxMoneyIndex]).ToString("0");
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
        if (!IsAI && AIInstance != null) Money.AIInstance.DeactivateMoney();
    }
    public void ActivateMoney() 
    {   
        _money_is_active = true; 
        if (!IsAI && AIInstance != null) Money.AIInstance.ActivateMoney();
    }
    public void ResetMoney()
    { 
        CurrentMoney = 0; 
        if (!IsAI && AIInstance != null) Money.AIInstance.ResetMoney();
    }
    
    public void MoneybuildingGen()
    {
        MoneyGainPerSec += 10;
    }
    
}