using UnityEngine;
using TMPro;
using System.Collections;


public class Money : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI moneyText;
    [SerializeField] FloatVariable genPerSec;
    [SerializeField] FloatVariable moneyAmount;

    void Start ()
    {
        StartCoroutine(moneyCount());
    }

    IEnumerator moneyCount()
    {
        while(moneyAmount._Value < 9999)
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
}

