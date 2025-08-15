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

    /// <summary>
    /// Maps an ID to a SceneActivity GameObject
    /// </summary>
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

    internal void Start()
    {
        Debug.Log("SceneActivityManager.Start", gameObject);

        CacheAllSAObjects();

        // Activate the 'Initial' SceneActivity and disable all others
        Activate(initialActivity, true);
    }

    void CacheAllSAObjects()
    {
        ClearCache();

        foreach (var obj in Resources.FindObjectsOfTypeAll<GameObject>())
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

    void OnDestroy()
    {
        Debug.Log("SceneActivityManager.OnDestroy", gameObject);
    }

    void ClearCache()
    {
        objNamed.Clear();
        initialActivity = null;
        //currentActivity = null;
    }

    void CacheSceneActivity(GameObject obj)
    {
        if (!objNamed.ContainsKey(obj.name))
        {
            Debug.Log($"Found SceneActivity \"{obj.name}\"", gameObject);
            objNamed.Add(obj.name, obj);
        }
        else
        {
            throw new SAException($"Name collision on '{obj.name}'");
        }
    }

    public void ActivateInitialSA() { Activate(initialActivity); }
    public void ActivatePreviousSA()
    {
        if (changeHistory.Count > 0)
        {
            string targetName = changeHistory.Pop();
            Activate(targetName);
        }
        else
        {
            Debug.LogWarning("No SceneActivity to go BACK to!, gameObject");
        }
    }
    public void ActivateSettings() { Activate("Settings"); }


    public void Activate(GameObject nextObj, bool disableAllOthers = false)
    {
        GameObject currObj = GetCurrentActivity();

        if (nextObj == null)
        {
            throw new SAException($"SceneActivity \"{name}\" was not found");
        }

        if (currObj == null)
        {
            nextObj.GetComponent<SceneActivity>().StartActivity();
            Debug.Log($"SceneActivityManager: -> {name}", nextObj);
        }
        else if (!currObj.Equals(nextObj))
        {
            nextObj.GetComponent<SceneActivity>().StartActivity();
            currObj.GetComponent<SceneActivity>().StopActivity();


            // Push the SAObject index into our changeHistory or
            // the strName if there is no index
            changeHistory.Push(currObj.name);
            Debug.Log($"SceneActivityManager: {currObj.name} -> {nextObj.name}", nextObj);
        }
        else
        {
            return;
        }

        currentActivity = nextObj;

        // Notify all interested parties that an SceneActivity
        // change has occurred.
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

    public void Activate(string name)
    {
        Activate(FindActivity(name));
    }

    public GameObject GetCurrentActivity()
    {
        return currentActivity;
    }

    /// <summary>
    /// Indicates if 'SA_0...' is currently active
    /// </summary>
    /// <returns>true if active, false if not</returns>
    public bool InInitialSA()
    {
        return initialActivity.Equals(currentActivity);
    }

    /// <summary>
    /// Returns the SceneActivity/GameObject with a specified name
    /// </summary>
    /// <param name="activityName">Either the int id as a string OR the detailed name</param>
    /// <returns>GameObject associated with the specified name</returns>
    public GameObject FindActivity(string activityName)
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
