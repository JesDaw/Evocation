using UnityEngine;

/// <summary>
/// Application-level lifecycle actions that don't belong to any single
/// component. Wire button OnClick/ButtonClicked events here instead of to
/// UI-visuals scripts like UIButtons.
///
/// Place this on a persistent GameObject (e.g. alongside GlobalInputManager).
/// </summary>
public static class ApplicationManager
{
    public static void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}