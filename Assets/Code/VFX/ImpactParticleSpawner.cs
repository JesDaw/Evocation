using System;
using System.Collections.Generic;
using UnityEngine;

public class ImpactParticleSpawner : MonoBehaviour
{
    public static ImpactParticleSpawner Instance { get; private set; }
    [SerializeField] private GameObject largeImpactParticleTemplate, smallImpactParticleTemplate;
    [SerializeField] private int pooledParticleCount = 20;
    private List<GameObject> _pooledLargeImpactParticles = new(), _pooledSmallImpactParticles = new();
    

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("duplicate impact particle controller found");
        }
    }

    private void Start()
    {
        for (int i = 0; i < pooledParticleCount; i++)
        {
            GameObject largeParticle = Instantiate(largeImpactParticleTemplate, transform);
            GameObject smallParticle = Instantiate(smallImpactParticleTemplate, transform);
            largeParticle.SetActive(false);
            smallParticle.SetActive(false);
            _pooledLargeImpactParticles.Add(largeParticle);
            _pooledSmallImpactParticles.Add(smallParticle);
        }
    }

    public void PlayLargeImpactParticle(Vector3 worldPosition, Vector3 scale, Quaternion localRotation)
    {
        GameObject impactParticle = GetLargeImpactParticle();
        if (impactParticle is not null)
        {
            impactParticle.transform.position = worldPosition;
            impactParticle.transform.localScale = scale;
            impactParticle.transform.localRotation = localRotation;
            impactParticle.SetActive(true);
        }
    }
    
    public void PlaySmallImpactParticle(Vector3 worldPosition, Vector3 scale, Quaternion localRotation)
    {
        GameObject impactParticle = GetSmallImpactParticle();
        if (impactParticle is not null)
        {
            impactParticle.transform.position = worldPosition;
            impactParticle.transform.localScale = scale;
            impactParticle.transform.localRotation = localRotation;
            impactParticle.SetActive(true);
        }
    }

    private GameObject GetLargeImpactParticle()
    {
        for(int i = 0; i < pooledParticleCount; i++)
        {
            if(!_pooledLargeImpactParticles[i].activeInHierarchy)
            {
                return _pooledLargeImpactParticles[i];
            }
        }
        return null;
    }
    
    private GameObject GetSmallImpactParticle()
    {
        for(int i = 0; i < pooledParticleCount; i++)
        {
            if(!_pooledSmallImpactParticles[i].activeInHierarchy)
            {
                return _pooledSmallImpactParticles[i];
            }
        }
        return null;
    }
}
