using UnityEngine;
using System; // To get access to dynamic arrays
using System.Collections;
using System.Collections.Generic;
[CreateAssetMenu(fileName = "RelationshipClass", menuName = "Scriptable Objects/PlayerRelationship")]
public class PlayerRelationshipSO : ScriptableObject
{
    [SerializeField] PlayerNPC_relations[] RelationStats;
}

[System.Serializable]
public class PlayerNPC_relations
{
    [SerializeField] string CharName = "None"; 
    [SerializeField] int Relationship_Quality = 0; 
    [SerializeField] int Depth_Level = 0; 

    public void CheckRQ(int Relationship_Quality, int Relationship_Depth_Level)
    {
        if (Relationship_Quality >= 100)
        {
            Depth_Level += 1;
            Relationship_Quality = 0;    
        }
        else if (Relationship_Quality < -100)
        {
            Relationship_Quality -= 100;
        }
    }
}


