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
    [SerializeField] float StartingMoney = 0;
    [HideInInspector] public float CurrentMoney = 0;
    [HideInInspector] public int CurrentMaxMoneyIndex = 0;
    [SerializeField] float[] MaxMoney = {200, 400, 600, 800, 1000};
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
        if (StartingMoney > MaxMoney[0]) StartingMoney = MaxMoney[0];
        CurrentMoney = StartingMoney;
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

    public void spendMoney(int amount)
    {
        CurrentMoney -= amount;
        MoneyUpdate();
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
    public void UpgradeMaxMoney()
    {
        if(MaxMoney.Length - 1 > CurrentMaxMoneyIndex)
        {
            CurrentMaxMoneyIndex += 1;
            UpdateMoneyGen();
            //effects;
        }
    }
    public void MoneybuildingGen()
    {
        MoneyGainPerSec += 10;
    }
    public void UpdateMoneyGen() 
    { 
        MoneyGainPerSec = InitialMoneyGainPerSec * (MaxMoney[CurrentMaxMoneyIndex]/MaxMoney[0]);
        if (DebugLogs) Debug.Log($"money gen per sec = {InitialMoneyGainPerSec} * {MaxMoney[CurrentMaxMoneyIndex]} / {MaxMoney[0]} = {MoneyGainPerSec}");

    }
}