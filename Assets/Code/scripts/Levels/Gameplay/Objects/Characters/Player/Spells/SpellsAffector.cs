using UnityEngine;
using UnityEngine.Events;

//scripts for the actual functions on hit on the specific spells
public class SpellsAffector : MonoBehaviour
{
    [SerializeField] bool DebugLogs = false;
    public void FireballSpell(Transform[] _enemiesHit)
    {
        if(DebugLogs) Debug.Log("<color=green>SPELLS--Fireball</color>");
        for (int I = 0; I < _enemiesHit.Length; I++)
        {
            _enemiesHit[I].GetComponent<DamageHandler>().TakeDamage(5);
        }
    }
    
    public void TempBuff(Transform[] _enemiesHit)
    {
        for (int I = 0; I < _enemiesHit.Length; I++)
        {
            if(DebugLogs) Debug.Log("<color=green>SPELLS--Status Increase</color>");
            _enemiesHit[I].GetComponent<DamageHandler>().TakeDamage(5);
        }
    }
}
