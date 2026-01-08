using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// IMPROVED MapZonesManager with AI-friendly methods
/// Add these methods to your existing MapZonesManager.cs
/// </summary>
public class MapZonesManager : MonoBehaviour
{
    [System.Serializable]
    public class TrackingPoint
    {
        public string tagToTrack;
        public Transform trackingPoint;
    }

    public List<TrackingPoint> trackingPoints = new List<TrackingPoint>();
    [SerializeField] GameObject AllyTracker;
    
    // Make this public so AI can access it!
    public Dictionary<string, List<GameObject>> trackedObjects = new Dictionary<string, List<GameObject>>();

    private void Start()
    {
        foreach (var point in trackingPoints)
        {
            if (!trackedObjects.ContainsKey(point.tagToTrack))
            {
                trackedObjects[point.tagToTrack] = new List<GameObject>();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (trackedObjects.ContainsKey(other.tag) && !trackedObjects[other.tag].Contains(other.gameObject))
        {
            trackedObjects[other.tag].Add(other.gameObject);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (trackedObjects.ContainsKey(other.tag))
        {
            trackedObjects[other.tag].Remove(other.gameObject);
        }
    }

    public GameObject GetClosestObject(string tag, Vector2 point)
    {
        if (!trackedObjects.ContainsKey(tag) || trackedObjects[tag].Count == 0)
            return null;

        GameObject closest = null;
        float minDistance = Mathf.Infinity;

        foreach (GameObject obj in trackedObjects[tag])
        {
            if (obj == null) continue; // Skip destroyed objects
            
            float distance = Vector2.Distance(point, obj.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                closest = obj;
            }
        }

        return closest;
    }

    public bool CompareSides()
    {
        float smallestDistance = Mathf.Infinity;
        string closestTrackerName = "";
        GameObject closestObject = null;

        foreach (var point in trackingPoints)
        {
            if (point.trackingPoint == null) continue;

            GameObject obj = GetClosestObject(point.tagToTrack, point.trackingPoint.position);
            if (obj != null)
            {
                float distance = Vector2.Distance(point.trackingPoint.position, obj.transform.position);
                if (distance < smallestDistance)
                {
                    smallestDistance = distance;
                    closestTrackerName = point.trackingPoint.name;
                    closestObject = obj;
                }
            }
        }
        
        int totalTrackedObjects = trackedObjects.Values.Sum(list => list.Count);
        Debug.Log($"Total objects being tracked: {totalTrackedObjects}");

        if (closestObject != null && closestTrackerName == AllyTracker.name)
        {
            Debug.Log($"Closest object is {closestObject.name}, tracked by {closestTrackerName} at distance {smallestDistance}");
            return true;
        }
        return false;
    }

    // ========== NEW METHODS FOR AI SYSTEM ==========
    
    /// <summary>
    /// Get count of objects with a specific tag in this zone
    /// Perfect for AI considerations!
    /// </summary>
    public int GetCountByTag(string tag)
    {
        if (!trackedObjects.ContainsKey(tag))
            return 0;
        
        // Clean up null objects
        trackedObjects[tag].RemoveAll(obj => obj == null);
        
        return trackedObjects[tag].Count;
    }
    
    /// <summary>
    /// Get all objects with a specific tag in this zone
    /// </summary>
    public List<GameObject> GetObjectsByTag(string tag)
    {
        if (!trackedObjects.ContainsKey(tag))
            return new List<GameObject>();
        
        // Clean up null objects and return copy
        trackedObjects[tag].RemoveAll(obj => obj == null);
        return new List<GameObject>(trackedObjects[tag]);
    }
    
    /// <summary>
    /// Get total count of ALL tracked objects in this zone
    /// </summary>
    public int GetTotalCount()
    {
        int total = 0;
        foreach (var list in trackedObjects.Values)
        {
            if (list != null)
                total += list.Count;
        }
        return total;
    }
    
    /// <summary>
    /// Check if zone is empty (no objects)
    /// </summary>
    public bool IsEmpty()
    {
        return GetTotalCount() == 0;
    }
    
    /// <summary>
    /// Check if a specific tag is being tracked
    /// </summary>
    public bool IsTrackingTag(string tag)
    {
        return trackedObjects.ContainsKey(tag);
    }
    
    /// <summary>
    /// Get ratio of one tag vs another (useful for dominance calculations)
    /// Returns 0-1 where 1 = all tag1, 0 = all tag2
    /// </summary>
    public float GetTagRatio(string tag1, string tag2)
    {
        int count1 = GetCountByTag(tag1);
        int count2 = GetCountByTag(tag2);
        int total = count1 + count2;
        
        if (total == 0) return 0.5f; // Neutral/empty
        
        return (float)count1 / total;
    }
    
    /// <summary>
    /// Get all tags currently being tracked with counts
    /// </summary>
    public Dictionary<string, int> GetAllTagCounts()
    {
        Dictionary<string, int> counts = new Dictionary<string, int>();
        
        foreach (var kvp in trackedObjects)
        {
            kvp.Value.RemoveAll(obj => obj == null);
            counts[kvp.Key] = kvp.Value.Count;
        }
        
        return counts;
    }
    
    /// <summary>
    /// Debug: Print all tracked objects
    /// </summary>
    [ContextMenu("Print Zone Contents")]
    public void PrintZoneContents()
    {
        Debug.Log($"=== {gameObject.name} Contents ===");
        
        foreach (var kvp in trackedObjects)
        {
            kvp.Value.RemoveAll(obj => obj == null);
            Debug.Log($"  {kvp.Key}: {kvp.Value.Count} objects");
        }
        
        Debug.Log($"Total: {GetTotalCount()} objects");
    }
}