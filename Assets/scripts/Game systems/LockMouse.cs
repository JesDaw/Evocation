using UnityEngine;


public class LockMouse : MonoBehaviour
{
    public bool Menu;
    void CurserControl () {
        if (Menu)
        {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        }

        if (!Menu)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }
    
}
