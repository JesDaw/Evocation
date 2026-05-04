using UnityEngine;

public class ManaObjectSpawn : MonoBehaviour
{
    [SerializeField] GameObject manaObject;
    GameObject LinkedManaObject;

    [SerializeField] float repeatDuration;

    void Start()
    {
        InvokeRepeating(nameof(SpawnMana), 0f, repeatDuration);
    }

    public void SpawnMana()
    {
        if(LinkedManaObject != null) return;
        LinkedManaObject = Instantiate(manaObject, transform.position, Quaternion.identity);
    }
}
