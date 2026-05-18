using UnityEngine;

public static class AreaEffectLogic
{
    public static AreaEffectZone SpawnZone(
        AreaEffectData data,
        Vector3 position,
        Stats caster,
        Transform stickyTarget,
        bool excludeCaster,
        bool? stickyOverride = null)
    {
        if (data == null) return null;

        GameObject zoneObj = new GameObject($"AreaEffect [{data.name}]");
        zoneObj.transform.position = position;

        AreaEffectZone zone = zoneObj.AddComponent<AreaEffectZone>();
        zone.Initialize(data, caster, stickyTarget, excludeCaster, stickyOverride);
        return zone;
    }
}