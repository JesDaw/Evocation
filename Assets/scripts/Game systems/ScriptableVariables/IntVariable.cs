using UnityEngine;
[CreateAssetMenu]
public class IntVeriable : ScriptableObject
{
    [SerializeField] int _BaseValue = 1;
    void OnEnable () { _Value =  _BaseValue;}
    public int _Value; 
}
