using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GlobalVolumeChanger : MonoBehaviour
{
    Volume volume;
    Vignette vignette;

    void Awake()
    {
        volume = GetComponent<Volume>();
    }

    public void SpellCast()
    {
        if (volume.profile.TryGet(out vignette))
        {
            vignette.intensity.value = 0.4f;
        }
    }

    public void ResetSpell()
    {
        if (volume.profile.TryGet(out vignette))
        {
            vignette.intensity.value = 0f;
        }
    }
}
