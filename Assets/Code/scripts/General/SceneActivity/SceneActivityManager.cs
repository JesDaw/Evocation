using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// This Script/Behavior handles all the SceneActivity transitions
/// for a given Scene.

/// </summary>
public class SceneActivityManager : MonoBehaviour
{
    public class SAException : Exception
    {
        public SAException(string msg)
        : base(msg)
        {
        }
    }

    internal GameObject currentActivity;

    internal GameObject initialActivity;
    internal GameObject anchorActivity;

    public GameObject[] SceneActivities;
    internal Dictionary<string, GameObject> objNamed = new Dictionary<string, GameObject>();
    internal Stack<string> changeHistory = new Stack<string>();
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

    internal void Start()
    {
        anchorActivity = initialActivity;
        SwapActivity(initialActivity, makeAnchor: false, disableAllOthers: true);
    }
   

    public void Activate(string name, bool makeAnchor = false)
    {
        Activate(FindActivityByName(name), makeAnchor);
    }

    public void Activate(GameObject nextObj, bool makeAnchor = false)
    {
        if (nextObj == null)
        {
            throw new SAException($"SceneActivity \"{name}\" was not found");
        }

        if (currentActivity == null || currentActivity.Equals(nextObj))
        {
            SwapActivity(nextObj, makeAnchor);
            return;
        }

        var outgoing = currentActivity.GetComponent<SceneActivity>();
        var incoming = nextObj.GetComponent<SceneActivity>();

        outgoing.PlayExitTransition(() =>
        {
            SwapActivity(nextObj, makeAnchor);
            incoming.PlayEnterTransition();
        });
    }
    void SwapActivity(GameObject nextObj, bool makeAnchor = false, bool disableAllOthers = false)
    {
        GameObject currObj = GetCurrentActivity();

        if (currObj == null)
        {
            nextObj.GetComponent<SceneActivity>().StartActivity();
        }
        else if (!currObj.Equals(nextObj))
        {
            currObj.GetComponent<SceneActivity>().StopActivity();
            nextObj.GetComponent<SceneActivity>().StartActivity(); 
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

        if (disableAllOthers)
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
    public void ActivatePreviousSA()
    {
        if (changeHistory.Count == 0)
        {
            Debug.LogWarning("No SceneActivity to go BACK to!", gameObject);
            return;
        }

        string targetName = changeHistory.Pop();
        isNavigatingBack = true;
        Activate(targetName, makeAnchor: false);
        isNavigatingBack = false;
    }
    public void ActivateAndMakeAnchor(string name) => Activate(name, makeAnchor: true);
    public void ActivateWithoutMakingAnchor(string name) => Activate(name, makeAnchor: false);

    public void ActivateSettings() => Activate("Settings");

    public void ActivateInitialSA() => Activate(initialActivity);

    public void ActivateAnchorSA() => Activate(anchorActivity);

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