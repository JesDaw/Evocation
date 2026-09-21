using UnityEngine;

public class RelationshipManager : MonoBehaviour
{

    public PlayerRelationshipSO playerRelationshipSO;
    public int CurrentRelationshipNumber = 0;
    public static RelationshipManager Instance { get; private set; }
     void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }
    public void ResetAllRelationships() => playerRelationshipSO.ResetAllRelationships();
    public void StartInteraction(int RelationshipNumber) => CurrentRelationshipNumber = RelationshipNumber;
    public void AlterRelationshipStatus(int amount)
    {
        playerRelationshipSO.RelationStats[CurrentRelationshipNumber].Relationship_Quality += amount;
        playerRelationshipSO.RelationStats[CurrentRelationshipNumber].CheckRQ();
    }
    
}
