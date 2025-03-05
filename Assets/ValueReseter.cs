using UnityEngine;
using UnityEngine.Events;

public class ValueReseter : MonoBehaviour
{
    [SerializeField] UnityEvent _ResetValues;
    public void ResetValues(){ _ResetValues.Invoke(); }
}
