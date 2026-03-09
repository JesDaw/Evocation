using UnityEngine;

//scripts for the actual functions on hit on the specific spells
public class SpellsAffector : MonoBehaviour
{
    public void FireballSpell(Transform[] _enemiesHit)
    {
        for (int I = 0; I < _enemiesHit.Length; I++)
        {
            _enemiesHit[I].GetComponent<DamageHandler>().TakeDamage(5);
        }
    }
    
    public void TempBuff(Transform[] _enemiesHit)
    {
        for (int I = 0; I < _enemiesHit.Length; I++)
        {
            _enemiesHit[I].GetComponent<DamageHandler>().TakeDamage(5);
        }
    }
}
