using UnityEngine;
using System; // To get access to dynamic arrays
using System.Collections;
using System.Collections.Generic;
[CreateAssetMenu(fileName = "RelationshipClass", menuName = "Scriptable Objects/PlayerRelationship")]
public class PlayerRelationshipSO : ScriptableObject
{
    public PlayerNPC_relations[] RelationStats;
    public void ResetAllRelationships()
    {
        foreach (var rel in RelationStats)
        {
            rel.Relationship_Quality = 0;
            rel.Depth_Level = 0;
        }
    }
}

[System.Serializable]
public class PlayerNPC_relations
{
    public string CharName = "None"; 
    public int Relationship_Quality = 0; 
    public int Depth_Level = 0; 
    public ClanStats clanStats;
    public void CheckRQ()
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


