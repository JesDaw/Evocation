using UnityEngine;
using TMPro;
using System.Collections;


public class Money : MonoBehaviour
{
    
    [SerializeField] FloatVariable genPerSec;
    [SerializeField] FloatVariable moneyAmount;
    public float CurrentMoney => moneyAmount._Value;
    GameObject moneyTextObj;
    TextMeshProUGUI moneyText;

    bool _money_is_active = true;

    void Awake()
    {
        moneyTextObj = GameObject.Find("MoneyText");
        if (moneyTextObj == null) Debug.LogError("MoneyManager could not find the MoneyText game object");
        moneyText = moneyTextObj.GetComponent<TextMeshProUGUI>();
    }

    void Start ()
    {
        StartCoroutine(moneyCount());
    }

    IEnumerator moneyCount()
    {
        while(moneyAmount._Value < 9999 && _money_is_active)
        {
            moneyAmount._Value++;
            UpdateMoneyDesplay();
            yield return new WaitForSeconds(1/genPerSec._Value);
        }
    }

    public void UpdateMoneyDesplay()
    {
        moneyText.text = moneyAmount._Value.ToString("0");
        Debug.Log("UpdateMoneyDesplay updated");
    }

    public void spendMoney(int amount)
    {
        moneyAmount._Value -= amount;
    }
    public void DeactivateMoney(){ _money_is_active = false; }
    public void ActivateMoney()
    {
        _money_is_active = true;
        StartCoroutine(moneyCount());
    }
    public void ResetMoney() { moneyAmount.Reset(); }
    public void IncreaseMoneyGen(){ genPerSec._Value *= 2; }
}
