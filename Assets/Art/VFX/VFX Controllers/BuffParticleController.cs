using System.Collections.Generic;
using UnityEngine;

public class BuffParticleController : MonoBehaviour
{
    [SerializeField] private ParticleSystem buffApplicationTemplate, softGlowPersistent, alphaSparklePersistent, additiveSparklePersistent;

    public enum BuffType
    {
        Type1,
        Type2,
        Type3,
    }

    private static readonly Dictionary<BuffType, Color> buffPrimaryColor = new Dictionary<BuffType, Color>
    {
        { BuffType.Type1, new Color(192/255f, 4/255f, 0)},
    };
    
    private static readonly Dictionary<BuffType, Color> buffSecondaryColor = new Dictionary<BuffType, Color>
    {
        { BuffType.Type1, new Color(1, 56/255f, 52/255f)},
    };

    public void StartBuffParticles(BuffType buffType)
    {
        GameObject buffApplicationObj =  Instantiate(buffApplicationTemplate.gameObject, transform.position, buffApplicationTemplate.transform.rotation, null);
        Destroy(buffApplicationObj, buffApplicationTemplate.main.duration);

        var buffApplicationPSChildren =  buffApplicationObj.GetComponentsInChildren<ParticleSystem>(includeInactive:false);
        foreach (ParticleSystem ps in buffApplicationPSChildren)
        {
            var main = ps.main;
            main.startColor = new ParticleSystem.MinMaxGradient(buffPrimaryColor[buffType], buffSecondaryColor[buffType]);
        }

        var glowMain = softGlowPersistent.main;
        glowMain.startColor = new ParticleSystem.MinMaxGradient(
            new Color(buffPrimaryColor[buffType].r, buffPrimaryColor[buffType].g, buffPrimaryColor[buffType].b, 0.15f),
            new Color(1, 1, 1, 0));

        var alphaSparkleMain = alphaSparklePersistent.main;
        var additiveSparkleMain = additiveSparklePersistent.main;
        alphaSparkleMain.startColor =
            new ParticleSystem.MinMaxGradient(buffPrimaryColor[buffType], buffSecondaryColor[buffType]);
        additiveSparkleMain.startColor = buffPrimaryColor[buffType];

        softGlowPersistent.Play(withChildren:true);
        
    }

    public void StopBuffParticles()
    {
        softGlowPersistent.Stop(withChildren:true, ParticleSystemStopBehavior.StopEmitting);
    }
}
