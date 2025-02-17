using UnityEngine;
using TMPro;
using System.Collections;


public class Money : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI moneyText;
    [SerializeField] float genPerSec = 1f;
    [SerializeField] int moneyAmount = 0;

    void Start ()
    {
        StartCoroutine(moneyCount());
    }

    IEnumerator moneyCount()
    {
        while(moneyAmount < 9999)
        {
            moneyAmount++;
            moneyText.text = moneyAmount.ToString("0");
            yield return new WaitForSeconds(1/genPerSec);
        }
    }

    public void spendMoney(int amount)
    {
        moneyAmount -= amount;
    }
}

