using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// This Script/Behavior handles all the SceneActivity transitions
/// for a given Scene.
/// 
/// There only needs to be one GameObject associated with this
/// behavior per Scene. 
/// </summary>
public class SceneActivityManager : MonoBehaviour
{
    [SerializeField] VisualEffectsManager visualEffectsManager;
    public class SAException : Exception
    {
        public SAException(string msg)
        : base(msg)
        {
        }
    }

    /// <summary>
    /// Currently active SceneActivity GameObject
    /// </summary>
    internal GameObject currentActivity;

    internal GameObject initialActivity;
    internal GameObject anchorActivity;

    /// <summary>
    /// Maps an ID to a SceneActivity GameObject
    /// </summary>
    public GameObject[] SceneActivities;
    internal Dictionary<string, GameObject> objNamed = new Dictionary<string, GameObject>();

    /// <summary>
    /// Stack of scene transitions needed for shifting 'Back'
    /// 
    /// I expect this stack to stay relatively small so that
    /// memory is not an issue...
    /// </summary>
    internal Stack<string> changeHistory = new Stack<string>();

    /// <summary>
    /// Event is raised at every SceneActivity transition
    /// (including the first one)
    /// </summary>
    public UnityEvent ActivityChanged;

    public GameObject InitialSA { get => initialActivity; }
    public GameObject CurrentSA { get => currentActivity; }
    public GameObject AnchorSA { get => anchorActivity; }

    // Flag to prevent adding to history when navigating back
    private bool isNavigatingBack = false;

    void OnEnable()
    {
        CacheAllSAObjects();
    }

    internal void Start()
    {
        anchorActivity = initialActivity;
        Activate(initialActivity, true);
    }

    void CacheAllSAObjects()
    {
        ClearCache();
        CheckForInitialSA();
    }
    void ClearCache()
    {
        objNamed.Clear();
        initialActivity = null;
    }

    void CheckForInitialSA()
    {
        foreach (var obj in SceneActivities)
        {
            SceneActivity sa = obj.GetComponent<SceneActivity>();
            if (sa != null)
            {
                if (sa.gameObject.activeInHierarchy)
                {
                    if (initialActivity == null)
                    {
                        initialActivity = obj;
                    }
                    else
                    {
                        throw new SAException($"Multiple \"initial\" detected; {initialActivity.name} + {obj.name}");
                    }
                }
                CacheSceneActivity(obj);
            }
        }
    }

    void CacheSceneActivity(GameObject obj)
    {
        if (!objNamed.ContainsKey(obj.name))
        {
            objNamed.Add(obj.name, obj);
        }
        else
        {
            throw new SAException($"Name collision on '{obj.name}' (meaning 2 scenes activities have the same name lol)");
        }
    }

    void OnDestroy()
    {
        
    }

    public void ActivateInitialSA() { Activate(initialActivity); }

    public void ActivateAnchorSA() { Activate(anchorActivity); }

    public void ActivatePreviousSA()
    {
        if (changeHistory.Count > 0)
        {
            string targetName = changeHistory.Pop();
            isNavigatingBack = true; 
            Activate(targetName, true);
            isNavigatingBack = false; 
        }
        else
        {
            Debug.LogWarning("No SceneActivity to go BACK to!", gameObject);
        }
    }

    public void ActivatePreviousSAWithFade() //animation logic should not be handled here it should be handles on the scene activity itself
    {
        if (changeHistory.Count > 0)
        {
            string targetName = changeHistory.Pop();
            isNavigatingBack = true; 
            FadeOutThenIn(FindActivityByName(targetName), .3f);
            isNavigatingBack = false; 
        }
        else
        {
            Debug.LogWarning("No SceneActivity to go BACK to!", gameObject);
        }
    }

    public void ActivateSettings() { Activate("Settings"); }
    public void ActivateSettingsWithFade() 
    { 
        FadeOutThenIn(FindActivityByName("Settings"), .3f);
    }


    public void Activate(GameObject nextObj, bool disableAllOthers = false, bool makeAnchor = false)
    {
        GameObject currObj = GetCurrentActivity();

        if (nextObj == null)
        {
            throw new SAException($"SceneActivity \"{name}\" was not found");
        }

        if (currObj == null)
        {
            nextObj.GetComponent<SceneActivity>().StartActivity();
        }
        else if (!currObj.Equals(nextObj))
        {
            nextObj.GetComponent<SceneActivity>().StartActivity();
            currObj.GetComponent<SceneActivity>().StopActivity();
            if (!isNavigatingBack)
            {
                changeHistory.Push(currObj.name);
            }
        }
        else
        {
            return;
        }

        currentActivity = nextObj;

        if (makeAnchor) anchorActivity = currentActivity;

        ActivityChanged.Invoke();

        if (disableAllOthers) // I think this logic can help up make little popups or scene activities on top of scene activities
        {
            var alreadyHandled = new HashSet<GameObject>();
            foreach (var obj in objNamed.Values)
            {
                if (!obj.Equals(currentActivity) && !alreadyHandled.Contains(obj))
                {
                    obj.GetComponent<SceneActivity>().StopActivity();
                    alreadyHandled.Add(obj);
                }
            }
        }
    }

    public void Activate(string name, bool makeAnchor = false)
    {
        GameObject activity = FindActivityByName(name);
        if (activity != null)
        {
            Activate(activity, makeAnchor: makeAnchor);
        }
        else
        {
            Debug.LogError($"[SceneActivityManager] Failed to find/activate '{name}'");
        }
    }

    public void ActivateAndMakeAnchor(string name)
    {
        Activate(FindActivityByName(name), makeAnchor: true);
    }

    public void ActivateWithFade(string name) // again all fade logic should be handles by the senceactivity scripts
    {
        if (currentActivity == null)
        {
            Activate(name, makeAnchor: false);
            return;
        }

        GameObject nextActivity = FindActivityByName(name);
        if (nextActivity == null)
        {
            Debug.LogError($"[SceneActivityManager] Failed to find/activate '{name}'");
            return;
        }
        FadeOutThenIn(nextActivity.gameObject, .3f);
    }

    void FadeOutThenIn(GameObject toObj, float duration) // ya this would be much easier within the scene activities, also It would be cool of we could point to different animations so what animation a screen has isnt hard coaded
    {
        if (currentActivity.gameObject == null || toObj == null) return;
        VisualEffectsManager.Instance.FadeOut(currentActivity.gameObject, duration, () =>
        {
            
            var cg = toObj.GetComponent<CanvasGroup>();
            if (cg != null) { cg.alpha = 0f; }
            cg = currentActivity.gameObject.GetComponent<CanvasGroup>();
            if (cg != null) { cg.alpha = 1f; }
            Activate(toObj, true);
            VisualEffectsManager.Instance.FadeIn(toObj, duration);
        });
    }

    // Unity seems to have a problem with optional arguments so this is a
    // convenience method capable of being referenced within the Unity GUI.
   

    public GameObject GetCurrentActivity()
    {
        return currentActivity;
    }
    public bool InitialSAIsActive()
    {
        return initialActivity.Equals(currentActivity);
    }
    public bool InAnchorSAIsActive()
    {
        return anchorActivity.Equals(currentActivity);
    }

    public GameObject FindActivityByName(string activityName)
    {
        GameObject result = null;

        if (objNamed.ContainsKey(activityName))
        {
            result = objNamed[activityName];
        }
        else
        {
            throw new SAException($"No SceneActivity named '{activityName}' was found");
        }

        return result;
    }
}