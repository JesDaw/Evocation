using UnityEngine;
using FMODUnity;

public class Test2 : MonoBehaviour
{
    public void PlayOneShot(EventReference sound)
    {
        RuntimeManager.PlayOneShot(sound);
    }
}
