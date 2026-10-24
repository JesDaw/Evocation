using System;
using UnityEngine;

public class BioluminescentParticleController : MonoBehaviour
{
    [SerializeField] Material bioluminescentMaterial;
    private ParticleSystem _ps;
    
    void Start()
    {
        float waveRate = bioluminescentMaterial.GetFloat("_BrightnessFluctuationRate");
        float period = Mathf.PI * 2f /waveRate;
        float offset = Mathf.PI * 11f / 6f;
        float currentTime = Time.time;

        float startDelay = offset - (Time.time % period);
        if (startDelay < 0f)
        {
            startDelay += period;
        }
        
        _ps = GetComponent<ParticleSystem>();
        var main = _ps.main;
        main.startDelay = startDelay;
        
        _ps.Play();
    }
}
