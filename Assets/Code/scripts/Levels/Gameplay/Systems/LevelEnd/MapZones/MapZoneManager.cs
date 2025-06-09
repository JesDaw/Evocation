using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class MapZonesManager : MonoBehaviour
{
    [System.Serializable]
    public class TrackingPoint
    {
        public string tagToTrack; // The tag this point is tracking
        public Transform trackingPoint; // The empty object used as a tracking position
    }

    public List<TrackingPoint> trackingPoints = new List<TrackingPoint>(); // List of tracking points
    [SerializeField] GameObject AllyTracker;
    private Dictionary<string, List<GameObject>> trackedObjects = new Dictionary<string, List<GameObject>>();

    private void Start()
    {
        // Initialize tracking lists for each tag
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

        // Output only the tracker with the smallest distance
        if (closestObject != null && closestTrackerName == AllyTracker.name)
        {
            Debug.Log($"Closest object is {closestObject.name}, tracked by {closestTrackerName} at distance {smallestDistance}");
            return true;
            
        }
        return false;
    }
}
