using UnityEngine;
[CreateAssetMenu]
public class GameObjectVeriable : ScriptableObject
{
    [SerializeField] GameObject _BaseValue;
    public GameObject _Value; 
    void OnEnable () { Reset(); }
    public void Reset() { _Value =  _BaseValue; }
    
}
