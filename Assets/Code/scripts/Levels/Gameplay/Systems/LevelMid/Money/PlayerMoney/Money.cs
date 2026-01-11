using UnityEngine;
using TMPro;
using System.Collections;

public class Money : MonoBehaviour
{
    [SerializeField] public FloatVariable genPerSec;
    [SerializeField] public FloatVariable moneyAmount;
    [SerializeField] TextMeshProUGUI moneyText;
    [SerializeField] AIMoneyManager aIMoneyManager;
    bool _money_is_active = false;
    float CurrentMoney = 0;
    
    public bool MoneyIsActive
    {
        get { return _money_is_active; }
    }

    void Awake()
    {
        CurrentMoney = moneyAmount._Value;

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
            if (!_money_is_active && moneyAmount._Value < 9999)
            {
                yield return null;
                continue;
            }
            moneyAmount._Value += 1;
            UpdateMoneyDesplay();
            yield return new WaitForSeconds(1/genPerSec._Value);
        }
    }

    public void UpdateMoneyDesplay()
    {
        moneyText.text = moneyAmount._Value.ToString("0");
        //Debug.Log("UpdateMoneyDesplay updated");
    }

    public void spendMoney(int amount)
    {
        moneyAmount._Value -= amount;
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
    public void ResetMoney() => moneyAmount.Reset(); 
    public void IncreaseMoneyGen() => genPerSec._Value *= 2; 
}