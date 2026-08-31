using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class PlayerDangerNotification : MonoBehaviour
{
    [SerializeField] Image[] notificationImages;
    [SerializeField] GameObject[] gameObjects;
    public static PlayerDangerNotification Instance {get; private set;}
    void Awake() => Instance = this;

    public IEnumerator ActivateForTime(float time)
    {
        Activate();
        yield return new WaitForSeconds(time);
        Deactivate();
    }

    public void Activate()
    {
        foreach (var gameObject in gameObjects) gameObject.SetActive(true);
        foreach (var image in notificationImages) image.enabled = true;
//        Debug.Log("activated");

    }

    public void Deactivate()
    {
        foreach (var gameObject in gameObjects) gameObject.SetActive(false);
        foreach (var image in notificationImages) image.enabled = false;
    }

}
