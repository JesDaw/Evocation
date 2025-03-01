using UnityEngine;

public class BuildingHealth : MonoBehaviour
{
    [SerializeField] FloatVariable Health;
   
    void TakeDamage(float damage)
    {
        Health._Value -= damage;
        // animation / effect 
    }
}
