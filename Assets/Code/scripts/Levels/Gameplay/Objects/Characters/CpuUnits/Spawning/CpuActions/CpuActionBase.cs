using System.Collections.Generic;
using System.Collections;
using UnityEngine;

[System.Serializable]
public abstract class BaseActionInfo : ScriptableObject
{
    public float ConditionLower;
    public float ConditionUpper;
    public float conditionRate;
}
