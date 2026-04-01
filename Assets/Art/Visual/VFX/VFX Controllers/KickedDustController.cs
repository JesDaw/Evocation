using System;
using UnityEngine;

public class KickedDustController : MonoBehaviour
{
    private void Awake()
    {
        var dustParticle = GetComponent<ParticleSystem>();
        GetComponentInParent<CpuStateManager>().OnCPUStateChange += state =>
        {
            if (state == CpuStateManager.State.Move)
            {
                dustParticle.Play(withChildren:true);
            }
            else
            {
                dustParticle.Stop(withChildren:true, ParticleSystemStopBehavior.StopEmitting);
            }
        };
    }
}
