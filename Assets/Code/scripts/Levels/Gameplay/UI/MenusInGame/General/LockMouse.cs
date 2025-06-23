using UnityEngine;

public class LockMouse : MonoBehaviour
{
    [SerializeField] bool MenuIsOpen;

    SceneActivityManager sceneMgr;

    void Start()
    {
        MenuIsOpen = false;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // Find the SceneActivityManager!
        foreach (var obj in Resources.FindObjectsOfTypeAll<SceneActivityManager>())
        {
            sceneMgr = obj;
        }
        Debug.Assert(sceneMgr != null);
    }
    public void OnEventRaised()
    {
        // The Initial SceneActivity for this Scene is the
        // GamePlayUI.  If we are in any other SA then we
        // are in some kind of Menu!
        bool isMenuCurrentlyOpen = !sceneMgr.InInitialSA();

        if (MenuIsOpen != isMenuCurrentlyOpen)
        {
            // Update our state
            MenuIsOpen = isMenuCurrentlyOpen;

            if (MenuIsOpen)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;

                Debug.Log("LockMouse -> Release Cursor");
            }
            else
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;

                Debug.Log("LockMouse -> Lock Cursor");
            }
        }
    }
}
