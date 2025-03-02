using UnityEngine;

public class LockMouse : MonoBehaviour
{
    [SerializeField] bool MenuIsOpen;

    void Start() 
    {
        MenuIsOpen = false;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
    public void OnEventRaised() 
    {
        MenuIsOpen = !MenuIsOpen;
        if (MenuIsOpen)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}
