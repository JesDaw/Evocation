using UnityEngine;
using System.Collections.Generic;

public class StatusEffectVisual : MonoBehaviour
{
    [SerializeField] private ParticleSystem buffApplicationTemplate;
    
    [Header("Persistent Effects")]
    [Tooltip("Add any particle systems here that should run for the duration of the effect.")]
    [SerializeField] private List<ParticleSystem> persistentParticles;

    public void Initialize(Color primary, Color secondary)
    {

        if (buffApplicationTemplate != null)
        {
            GameObject burstObj = Instantiate(buffApplicationTemplate.gameObject, transform.position, buffApplicationTemplate.transform.rotation);
            Destroy(burstObj, buffApplicationTemplate.main.duration);

            var burstChildren = burstObj.GetComponentsInChildren<ParticleSystem>(includeInactive: false);
            foreach (ParticleSystem ps in burstChildren)
            {
                var main = ps.main;
                main.startColor = new ParticleSystem.MinMaxGradient(primary, secondary);
            }
        }

        foreach (var ps in persistentParticles)
        {
            if (ps == null) continue;

            var main = ps.main;
            main.startColor = new ParticleSystem.MinMaxGradient(primary, secondary);
            
            ps.Play(withChildren: true);
        }
    }

    public void StopVisuals()
    {
        foreach (var ps in persistentParticles)
        {
            if (ps != null)
                ps.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
        }

        Destroy(gameObject, 5f);
    }

    void OnDestroy()
    {
        gameObject.SetActive(false);
    }
}