using UnityEngine;
using TMPro;
using System.Collections;


public class Money : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI moneyText;
    [SerializeField] FloatVariable genPerSec;
    [SerializeField] FloatVariable moneyAmount;
    public float CurrentMoney => moneyAmount._Value;

    bool _game_is_active = true;

    void Start ()
    {
        StartCoroutine(moneyCount());
    }

    IEnumerator moneyCount()
    {
        while(moneyAmount._Value < 9999 && _game_is_active)
        {
            moneyAmount._Value++;
            moneyText.text = moneyAmount._Value.ToString("0");
            yield return new WaitForSeconds(1/genPerSec._Value);
        }
    }

    public void spendMoney(int amount)
    {
        moneyAmount._Value -= amount;
    }
    public void DeactivateMoney(){ _game_is_active = false; }
    public void ResetMoney(){ moneyAmount.Reset();}
    public void IncreaseMoneyGen(){ genPerSec._Value *= 2; }
}
