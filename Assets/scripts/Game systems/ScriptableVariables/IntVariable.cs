using UnityEngine;
[CreateAssetMenu]
public class IntVeriable : ScriptableObject
{
    [SerializeField] int _BaseValue = 1;
    void OnEnable () { Reset(); }
    void Reset() { _Value =  _BaseValue; }
    public int _Value; 
}
