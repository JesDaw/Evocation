using System.Collections.Generic;
using UnityEngine;

public class AnimationDrivenVFXController : MonoBehaviour
{
    void SpawnVFX(int index)
    {
        Stats st = GetComponentInParent<Stats>();
        var toSpawn = st.scriptableStats.vfx[index];
        var newFX = Instantiate(toSpawn, transform.position, toSpawn.transform.rotation, null);
        if (st._Enemy)
        {
            newFX.transform.position += new Vector3(-st.scriptableStats.vfxOffsets[index].x, st.scriptableStats.vfxOffsets[index].y, 0);
            newFX.transform.Rotate(0, 180, 0);
        }
        else
        {
            newFX.transform.position += new Vector3(st.scriptableStats.vfxOffsets[index].x, st.scriptableStats.vfxOffsets[index].y, 0);
        }
        ParticleSystem p = newFX.GetComponent<ParticleSystem>();
        Destroy(newFX, p!=null ? p.main.duration : 5);
    }
}
