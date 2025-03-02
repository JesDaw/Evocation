using UnityEngine;
[CreateAssetMenu]

public class BoolVariable : ScriptableObject
{
    [SerializeField] bool _BaseValue = true;
    void OnEnable () { Reset(); }
    void Reset() { _Value =  _BaseValue; }
    public bool _Value; 
}
