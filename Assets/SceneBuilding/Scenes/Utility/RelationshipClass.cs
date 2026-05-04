using UnityEngine;
using System; // To get access to dynamic arrays
using System.Collections;
using System.Collections.Generic;

public class RelationshipClass : MonoBehaviour
{
    //Variable of the array
    [SerializeField]
    List<PlayerNPC_relations> RelationStats = new List<PlayerNPC_relations>();
}

[System.Serializable]
public class PlayerNPC_relations
{
    [SerializeField]
    string CharName = "None"; //The name of the character
    [SerializeField]
    int Relationship_Quality = 0; //Relationship Quality
    [SerializeField]
    int Depth_Level = 0; //Depth level

    public int CheckRQ(int RQ, int DL)
    {
        if (RQ >= 100)
        {
            //depth level is meant to increse by 1
            DL = DL + 1;
            //reset 
            RQ = 0;    
        }
        else if (RQ < -100)
        {
            RQ = -100;
        }

        return RQ;
    }
}


