using UnityEngine;

public static class SingletonManager
{
    public static bool Initialize<T>(T caller, ref T instanceField) where T : MonoBehaviour
    {
        if (instanceField != null && instanceField != caller)
        {
            Object.Destroy(caller.gameObject);
            return false;
        }
        instanceField = caller;
        return true;
    }
}


public static class ValidationUtilities
{
    public static bool CheckMonoBehaviours(MonoBehaviour caller, params MonoBehaviour[] references)
    {
        bool allValid = true;

        foreach (var script in references)
        {
            if (script == null)
            {
                Debug.LogWarning($"[{caller.GetType().Name}] A required MonoBehaviour script wasn't found in the scene!", caller);
                allValid = false;
            }
        }

        return allValid;
    }
}

public static class LoggerUtilities
{
    public static void Log(this MonoBehaviour caller, string message)
    {
        Debug.Log($"[{caller.GetType().Name}] {message}", caller);
    }
    public static void LogWarning(this MonoBehaviour caller, string message)
    {
        Debug.LogWarning($"[{caller.GetType().Name}] {message}", caller);
    }
    public static void LogError(this MonoBehaviour caller, string message)
    {
        Debug.LogError($"[{caller.GetType().Name}] {message}", caller);
    }
}


