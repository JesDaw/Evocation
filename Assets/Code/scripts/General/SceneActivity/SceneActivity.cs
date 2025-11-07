using UnityEngine;
using UnityEngine.Events;

public class SceneActivity : MonoBehaviour
{
    [SerializeField] public bool disableDefaultBehavior;

    public UnityEvent OnActivityStart;
    public UnityEvent OnActivityStop;

    public void StartActivity()
    {
        if (!disableDefaultBehavior)
        {
            gameObject.SetActive(true); 

        }
        OnActivityStart.Invoke();
    }

    public void StopActivity()
    {
        if (!disableDefaultBehavior)
        {
            gameObject.SetActive(false); 
        }
        OnActivityStop.Invoke();
    }
}
