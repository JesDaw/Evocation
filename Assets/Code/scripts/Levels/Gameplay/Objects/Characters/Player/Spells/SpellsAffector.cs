using System.Collections;
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
            DamageHandler currentDamager = _enemiesHit[I].GetComponent<DamageHandler>();
            if(_enemiesHit[I].CompareTag("Allies")) continue;
            if(_enemiesHit[I].CompareTag("Player")) continue;
            if(currentDamager == null) continue;
            currentDamager.TakeDamage(20, new DamageSource(DamageSource.DamageType.Spell));
        }
    }

    public void BiggerFireballSpell(Transform[] _enemiesHit)
    {
        if(DebugLogs) Debug.Log("<color=green>SPELLS--Fireball</color>");
        for (int I = 0; I < _enemiesHit.Length; I++)
        {
            DamageHandler currentDamager = _enemiesHit[I].GetComponent<DamageHandler>();
            if(_enemiesHit[I].CompareTag("Allies")) continue;
            if(_enemiesHit[I].CompareTag("Player")) continue;
            if(currentDamager == null) continue;
            currentDamager.TakeDamage(999, new DamageSource(DamageSource.DamageType.Spell));
        }
    }
    
    public void TempBuff(Transform[] _enemiesHit)
    {
        for (int I = 0; I < _enemiesHit.Length; I++)
        {
            if(DebugLogs) Debug.Log("<color=green>SPELLS--Status Increase</color>");
            DamageHandler currentDamager = _enemiesHit[I].GetComponent<DamageHandler>();
            if(currentDamager == null) continue;
            currentDamager.TakeDamage(5);
        }
    }
}
