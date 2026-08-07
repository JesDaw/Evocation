using UnityEngine;

public class ManaSpawner : MonoBehaviour
{
    [SerializeField] GameObject manaObjectVisual;
    [SerializeField] ParticleSystem collectEffect;
    [SerializeField] float minRepeatDuration = 1f;
    [SerializeField] float maxRepeatDuration = 2f;
    [SerializeField]int minManaAmount = 10;
    [SerializeField]int maxManaAmount = 11;
    bool manaIsCollectable = false;
    bool playerIsInCollectionRange = false;
    float timer = 0;
    bool countTimer = false;
    float repeatDuration;

    void Start()
    {
        if (minManaAmount >= maxManaAmount)
        {
            maxManaAmount = minManaAmount + 1;
            Debug.Log($"invalid max mana amount changing it to minManaAmount + 1 to always give {minManaAmount} mana");
        }
        if (minRepeatDuration >= maxRepeatDuration)
        {
            maxRepeatDuration = minRepeatDuration + 1;
            Debug.Log($"invalid maxRepeatDuration amount changing it to minRepeatDuration + 1 to always give {minRepeatDuration} mana");
        }
        if (manaIsCollectable == false)
        {
            manaObjectVisual.SetActive(false);
        }
        else
        {
            manaObjectVisual.SetActive(true);
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
                SpawnMana();
                
            }
        }
    }

    public void SpawnMana()
    {
        if (manaIsCollectable) return;
        if(manaObjectVisual == null) return;
        manaObjectVisual.SetActive(true);
        manaIsCollectable = true;
        if (playerIsInCollectionRange) Collectmana();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerIsInCollectionRange = true;
            if (manaIsCollectable) Collectmana();
        }
    }

    void Collectmana()
    {
        FModAudioManager.instance.PlaySoundByName("spawnTroop");
        collectEffect.Play();
        ManaSystem.Instance.IncreaseMana(Random.Range(minManaAmount, maxManaAmount));
        manaObjectVisual.SetActive(false);
        manaIsCollectable = false;
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
