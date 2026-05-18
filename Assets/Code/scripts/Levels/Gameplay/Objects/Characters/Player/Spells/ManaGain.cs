using UnityEngine;

public class ManaGain : MonoBehaviour
{
    [SerializeField] int manaAmount = 10;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            ManaSystem.Instance.IncreaseMana(manaAmount);
            Destroy(gameObject);
        }
    }
}
