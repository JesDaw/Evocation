using UnityEngine;

public class ResourceSpawner : MonoBehaviour
{
    [SerializeField] GameObject resourceObjectVisual;
    [SerializeField] ParticleSystem collectEffect;
    [SerializeField] float minRepeatDuration = 1f;
    [SerializeField] float maxRepeatDuration = 2f;
    [SerializeField]int minResourceAmount = 10;
    [SerializeField]int maxResourceAmount = 11;
    [SerializeField] ResourceType resourceType;
    [SerializeField] string collectionSound = "spawnTroop";
    bool resourceIsCollectable = false;
    bool playerIsInCollectionRange = false;
    float timer = 0;
    bool countTimer = false;
    float repeatDuration;

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
