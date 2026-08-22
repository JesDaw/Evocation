using UnityEngine;

public class ResourceSpawner : MonoBehaviour
{
    [SerializeField] GameObject resourceObjectVisual;
    [SerializeField] ParticleSystem collectEffect;
    [SerializeField] float minRepeatDuration = 1f;
    float originalMinRepeatDuration = 1f;
    [SerializeField] float maxRepeatDuration = 2f;
    float originalMaxRepeatDuration = 2f;
    [SerializeField]int minResourceAmount = 10;
    int originalMinResourceAmount = 10;
    [SerializeField]int maxResourceAmount = 11;
    int originalMaxResourceAmount = 11;
    public ResourceType resourceType;
    ResourceType originalResourceType;
    [SerializeField] string collectionSound = "spawnTroop";
    [SerializeField] bool DebugLogs;
    bool resourceIsCollectable = false;
    bool playerIsInCollectionRange = false;
    float timer = 0;
    bool countTimer = false;
    float repeatDuration;

    void Awake()
    {
        originalMinRepeatDuration = minRepeatDuration;
        originalMaxRepeatDuration = maxRepeatDuration;
        originalMinResourceAmount = minResourceAmount;
        originalMaxResourceAmount = maxResourceAmount;
        originalResourceType = resourceType;
    }

    void Start()
    {
        if (minResourceAmount >= maxResourceAmount)
        {
            maxResourceAmount = minResourceAmount + 1;
            Debug.Log($"invalid max Resource amount changing it to minManaAmount + 1 to always give {minResourceAmount} mana");
        }
        if (minRepeatDuration >= maxRepeatDuration)
        {
            maxRepeatDuration = minRepeatDuration + 1;
            Debug.Log($"invalid maxRepeatDuration amount changing it to minRepeatDuration + 1 to always give {minRepeatDuration} mana");
        }
        if (resourceIsCollectable == false)
        {
            resourceObjectVisual.SetActive(false);
        }
        else
        {
            resourceObjectVisual.SetActive(true);
        }
        countTimer = true;
    }

    public void ChangeData(ResourceChange resourceChange)
    {
        minRepeatDuration = Mathf.FloorToInt(minRepeatDuration * resourceChange.SpawnRateMultiplier);
        maxRepeatDuration = Mathf.FloorToInt(maxRepeatDuration * resourceChange.SpawnRateMultiplier);
        minResourceAmount = Mathf.FloorToInt(minResourceAmount * resourceChange.ValueMultiplier);
        maxResourceAmount = Mathf.FloorToInt(maxResourceAmount * resourceChange.ValueMultiplier);
        resourceType = resourceChange.ResourceTypeToChangeInto;
        if (DebugLogs) Debug.Log($"ChangeData old minResourceAmount = {originalMinResourceAmount} new = {minResourceAmount}");
    }
    public void RevertData()
    {
        minRepeatDuration = originalMinRepeatDuration;
        maxRepeatDuration =  originalMaxRepeatDuration;
        minResourceAmount  = originalMinResourceAmount;
        maxResourceAmount =  originalMaxResourceAmount;
        resourceType = originalResourceType;
        if (DebugLogs) Debug.Log($"Data Reverted");
    }

    void Update()
    {
        if (countTimer) 
        {
            timer += Time.deltaTime;
            if (timer >= repeatDuration)
            {
                countTimer = false;
                timer = 0;
                SpawnResource();
            }
        }
    }

    public void SpawnResource()
    {
        if (resourceIsCollectable) return;
        if(resourceObjectVisual == null) return;
        resourceObjectVisual.SetActive(true);
        resourceIsCollectable = true;
        if (playerIsInCollectionRange) CollectResource();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerIsInCollectionRange = true;
            if (resourceIsCollectable) CollectResource();
        }
    }

    void CollectResource()
    {
        FModAudioManager.instance.PlaySoundByName(collectionSound);
        collectEffect.Play();

        if (resourceType == ResourceType.Mana) ManaSystem.Instance.IncreaseMana(Random.Range(minResourceAmount, maxResourceAmount));
        if (resourceType == ResourceType.Money) Money.Instance.AddMoney(Random.Range(minResourceAmount, maxResourceAmount));

        resourceObjectVisual.SetActive(false);
        resourceIsCollectable = false;
        repeatDuration = Random.Range(minRepeatDuration, maxRepeatDuration);
        countTimer = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerIsInCollectionRange = false;
        }
    }
}

public enum ResourceType
{
    Money,
    Mana,
}
