using System.Collections.Generic;
using UnityEngine;

public static class AreaEffectLogic
{
    public static AreaEffectZone SpawnZone(
        AreaEffectData data,
        Vector3 position,
        Stats caster,
        Transform stickyTarget,
        bool excludeCaster,
        bool? stickyOverride = null,
        List<string> targetTags = null)
    {
        if (data == null)
        {
            Debug.LogWarning($"[Zone] SpawnZone called with null data from {caster?.gameObject.name ?? "UNKNOWN"}");
            return null;
        }

        GameObject zoneObj = new GameObject($"AreaEffect [{data.name}]");
        zoneObj.transform.position = position;

        AreaEffectZone zone = zoneObj.AddComponent<AreaEffectZone>();
        zone.Initialize(data, caster, stickyTarget, excludeCaster, stickyOverride, targetTags);

        string tagStr = targetTags != null ? string.Join(",", targetTags) : "null";
//        Debug.Log($"[Zone] Spawned '{data.name}' at {position}, tags=[{tagStr}], excludeCaster={excludeCaster}, sticky={zone.IsSticky}");

        return zone;
    }
}