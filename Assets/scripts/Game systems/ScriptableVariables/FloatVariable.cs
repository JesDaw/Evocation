using UnityEngine;
[CreateAssetMenu]
public class FloatVariable : ScriptableObject
{
    [SerializeField] float _BaseValue = 600;
    void OnEnable () { Reset(); }
    public void Reset() { _Value =  _BaseValue; }
    public float _Value; 
}
