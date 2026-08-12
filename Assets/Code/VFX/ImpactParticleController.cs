using System;
using UnityEngine;

public class ImpactParticleController : MonoBehaviour
{
    [SerializeField] private ParticleSystem mainParticle;
    private float elapsedTime = 0;

    private void Update()
    {
        elapsedTime += Time.deltaTime;
        if (elapsedTime > mainParticle.main.duration)
        {
            gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        mainParticle.Play();
    }

    private void OnDisable()
    {
        elapsedTime = 0;
    }
}
