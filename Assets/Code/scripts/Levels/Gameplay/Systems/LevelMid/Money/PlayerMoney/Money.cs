using UnityEngine;
using TMPro;
using System.Collections;

public class Money : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI moneyText;
    [SerializeField] AIMoneyManager aIMoneyManager;
    bool _money_is_active = false;
    public float CurrentMoney = 0;
    public float MoneyGainPerSec = 1; 
    
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
    }

    void Start()
    {
        StartCoroutine(moneyCount());
    }

    IEnumerator moneyCount()
    {
        while(true)
        {
            if (!_money_is_active && CurrentMoney < 9999)
            {
                yield return null;
                continue;
            }
            CurrentMoney += 1;
            UpdateMoneyDesplay();
            yield return new WaitForSeconds(1/MoneyGainPerSec);
        }
    }

    public void UpdateMoneyDesplay()
    {
        moneyText.text = CurrentMoney.ToString("0");
        //Debug.Log("UpdateMoneyDesplay updated");
    }

    public void spendMoney(int amount)
    {
        CurrentMoney -= amount;
        UpdateMoneyDesplay();
    }
    
    public void DeactivateMoney() 
    { 
        _money_is_active = false; 
        aIMoneyManager.DeactivateMoney();
    }
    public void ActivateMoney() 
    { 
        _money_is_active = true; 
        aIMoneyManager.ActivateMoney();
    }
    public void ResetMoney() => CurrentMoney = 0; 
    public void IncreaseMoneyGen() => MoneyGainPerSec *= 2; 
}