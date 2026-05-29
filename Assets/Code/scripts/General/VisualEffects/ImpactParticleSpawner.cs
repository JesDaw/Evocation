using System;
using System.Collections.Generic;
using UnityEngine;

public class ImpactParticleSpawner : MonoBehaviour
{
    public static ImpactParticleSpawner Instance { get; private set; }
    [SerializeField] private GameObject largeImpactParticleTemplate;
    [SerializeField] private int pooledParticleCount = 20;
    private List<GameObject> _pooledLargeImpactParticles = new();
    

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
            GameObject toAdd = Instantiate(largeImpactParticleTemplate, transform);
            var v = toAdd.GetComponent<ImpactParticleController>();
            toAdd.SetActive(false);
            _pooledLargeImpactParticles.Add(toAdd);
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
}
