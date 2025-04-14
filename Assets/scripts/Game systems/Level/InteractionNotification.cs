using UnityEngine;

public class InteractionNotification : MonoBehaviour
{
    [SerializeField] GameObject Icon;
    bool _isActive = false;


    public void ToggleIcon()
    {
        _isActive = !_isActive;
        Icon.SetActive(_isActive);
    }

}
