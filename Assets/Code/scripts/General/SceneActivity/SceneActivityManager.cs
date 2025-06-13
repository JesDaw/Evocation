using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
    /// <summary>
    /// Currently active SceneActivity GameObject
    /// </summary>
    GameObject currentActivity;

    /// <summary>
    /// Maps an ID to a SceneActivity GameObject
    /// </summary>
    Dictionary<string, GameObject> objNamed = new Dictionary<string, GameObject>();

    /// <summary>
    /// Stack of scene transitions needed for shifting 'Back'
    /// 
    /// I expect this stack to stay relatively small so that
    /// memory is not an issue...
    /// </summary>
    Stack<string> changeHistory = new Stack<string>();

    /// <summary>
    /// Event is raised at every SceneActivity transition
    /// (including the first one)
    /// </summary>
    public UnityEvent ActivityChanged;

    /// <summary>
    /// Parse a SceneActivity Object Name
    /// 
    /// A properly formatted name has one of the following forms:
    /// <list type="number">
    /// <item><description>"SA_[int]_[any characters]</description></item>
    /// <item><description>"SA_[any characters]</description></item>
    /// </list>
    /// 
    /// The first form should be used for SceneActivity objects
    /// with uncommon names - names not common to virtually every
    /// Scene.  The second form is reserved for common, widespread
    /// objects like "SA_Settings".
    /// </summary>
    /// <param name="aName">SA GameObject name to parse</param>
    /// <returns>(int? index, string strName)</returns>
    static public (int? index, string strName) ParseSAObjectName(string aName)
    {
        (int? index, string name) result = (null, null);

        var singleIdRegExp = new Regex(@"^SA_(.+)$");
        var dualIdRegExp = new Regex(@"^SA_(\d+)_(.+)$");

        Match match = dualIdRegExp.Match(aName);
        if (match.Success)
        {
            result = (int.Parse(match.Groups[1].Value), match.Groups[2].Value);
        }
        else
        {
            match = singleIdRegExp.Match(aName);
            if (match.Success)
            {
                result = (null, match.Groups[1].Value);
            }
        }

        return result;
    }

    static public bool IsSAObjectName(string aName)
    {
        var nameParts = aName.Split("_");
        return (nameParts.Length > 1 && nameParts[0] == "SA");
    }

    static public bool HasSAObjectName(GameObject obj)
    {
        return IsSAObjectName(obj.name);
    }

    void Start()
    {
        Debug.Log("SceneActivityManager.Start");

        // Cache all SceneActivity Objects
        foreach (var obj in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (HasSAObjectName(obj))
            {
                var name = ParseSAObjectName(obj.name);
                if (obj.GetComponent<SceneActivity>() != null)
                {
                    if (name.index.HasValue)
                    {
                        cacheSceneActivity($"{name.index}", obj);
                    }

                    if (name.strName != null)
                    {
                        cacheSceneActivity(name.strName, obj);
                    }
                }
                else
                {
                    Debug.LogError($"{obj.name} is NOT a valid SceneActivity (i.e. Doesn't have the MonoBehaviour)");
                }
            }
        }

        // Activate the 'Initial' SceneActivity and disable all others
        ChangeActivity(0, true);
    }

    void OnDestroy()
    {
        Debug.Log("SceneActivityManager.OnDestroy");
    }

    void cacheSceneActivity(string id, GameObject obj)
    {
        Debug.Assert(!objNamed.ContainsKey(id), "Ambiguous SA Names were used; id \"{id}\" has already been used!");
        if (!objNamed.ContainsKey(id))
        {
            objNamed.Add(id, obj);
        }
        else
        {
            Debug.LogError($"Duplicate SceneActivity named {id}");
        }
    }

    public void ActivateInitialSA() { ActivateSA0(); }

    public void ActivateSA0() { ChangeActivity(0); }

    public void ActivateSA1() { ChangeActivity(1); }

    public void ActivateSA2() { ChangeActivity(2); }

    public void ActivateSA3() { ChangeActivity(3); }

    public void ActivateSA4() { ChangeActivity(4); }

    public void ActivateSA5() { ChangeActivity(5); }


    public void ActivateSA6() { ChangeActivity(6); }


    public void ActivateSA7() { ChangeActivity(7); }


    public void ActivateSA8() { ChangeActivity(8); }

    public void ActivateSA9() { ChangeActivity(9); }

    public void ActivateSettings() { ChangeActivity("Settings"); }

    public void ActivateByName(string name)
    {
        ChangeActivity(name);
    }

    public void ActivateBack()
    {
        if (changeHistory.Count > 0)
        {
            string targetName = changeHistory.Pop();
            ChangeActivity(targetName);
        }
        else
        {
            Debug.LogError("No SceneActivity to go BACK to!");
        }
    }

    void ChangeActivity<T>(T activityId, bool disableAllOthers = false)
    {
        GameObject currObj = GetCurrentActivity();
        GameObject nextObj = FindActivity($"{activityId}");

        if (currObj == null)
        {
            nextObj.GetComponent<SceneActivity>().StartActivity();
            Debug.Log($"SceneActivityManager: -> {activityId}");
        }
        else if (!currObj.Equals(nextObj))
        {
            nextObj.GetComponent<SceneActivity>().StartActivity();
            currObj.GetComponent<SceneActivity>().StopActvity();
            var parsedName = ParseSAObjectName(currObj.name);

            // Push the SAObject index into our changeHistory or
            // the strName if there is no index
            changeHistory.Push($"{(parsedName.index.HasValue ? parsedName.index.Value : parsedName.strName)}");
            Debug.Log($"SceneActivityManager: {currObj.name} -> {nextObj.name}");
        }
        else
        {
            return;
        }

        currentActivity = nextObj;

        ActivityChanged.Invoke();

        if (disableAllOthers)
        {
            foreach (var obj in objNamed.Values)
            {
                if (!obj.Equals(currentActivity))
                {
                    obj.GetComponent<SceneActivity>().StopActvity();
                }
            }
        }
    }

    public GameObject GetCurrentActivity()
    {
        return currentActivity;
    }

    public bool InInitialSA()
    {
        bool result = false;

        if (currentActivity != null)
        {
            var saName = ParseSAObjectName(currentActivity.name);
            result = ((saName.index.HasValue) && (saName.index.Value == 0));
        }

        return result;
    }

    /// <summary>
    /// Returns the SceneActivity/GameObject with a specified name
    /// </summary>
    /// <param name="activityName">Either the int id as a string OR the detailed name</param>
    /// <returns>GameObject associated with the specified name</returns>
    public GameObject FindActivity(string activityName)
    {
        GameObject result = null;

        Debug.Assert(objNamed.ContainsKey(activityName), $"Reference to an unknown SceneActivity \"{activityName}\"");

        if (objNamed.ContainsKey(activityName))
        {
            result = objNamed[activityName];
        }

        return result;
    }
}
