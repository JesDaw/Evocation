using UnityEngine;
using UnityEngine.Events;

public class SceneActivity : MonoBehaviour
{
    [SerializeField] public bool disableDefaultBehavior;

    public UnityEvent OnActivityStart;
    public UnityEvent OnActivityStop;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    void OnDestroy()
    {

    }

    public void StartActivity()
    {
        if (!disableDefaultBehavior)
        {
            Debug.Log($"Setting {gameObject.name} to true");
            gameObject.SetActive(true); 

        }
        OnActivityStart.Invoke();
    }

    public void StopActivity()
    {
        if (!disableDefaultBehavior)
        {
            Debug.Log($"Setting {gameObject.name} to false");
            gameObject.SetActive(false); 
        }
        OnActivityStop.Invoke();
    }
}
