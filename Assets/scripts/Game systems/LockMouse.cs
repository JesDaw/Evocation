using UnityEngine;

public class LockMouse : MonoBehaviour
{
    public BoolVariable IsMenuOpen;

    void Update()
    {
        CursorControl();
    }

    void CursorControl() 
    {
        if (IsMenuOpen._Value)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }
}
