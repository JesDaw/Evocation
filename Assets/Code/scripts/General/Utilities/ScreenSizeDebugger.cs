using UnityEngine;

[ExecuteAlways]
public class ScreenSizeDebugger : MonoBehaviour
{
    public Camera cam;
    private Renderer rend;

    void Start()
    {
        rend = GetComponent<Renderer>();
    }

    void Update()
    {
        if (cam == null || rend == null) return;

        Bounds b = rend.bounds;

        // 8 corners of the bounding box
        Vector3[] corners = new Vector3[8];
        corners[0] = new Vector3(b.min.x, b.min.y, b.min.z);
        corners[1] = new Vector3(b.max.x, b.min.y, b.min.z);
        corners[2] = new Vector3(b.min.x, b.max.y, b.min.z);
        corners[3] = new Vector3(b.max.x, b.max.y, b.min.z);
        corners[4] = new Vector3(b.min.x, b.min.y, b.max.z);
        corners[5] = new Vector3(b.max.x, b.min.y, b.max.z);
        corners[6] = new Vector3(b.min.x, b.max.y, b.max.z);
        corners[7] = new Vector3(b.max.x, b.max.y, b.max.z);

        Vector3 minScreen = cam.WorldToScreenPoint(corners[0]);
        Vector3 maxScreen = minScreen;

        foreach (var c in corners)
        {
            Vector3 screen = cam.WorldToScreenPoint(c);
            minScreen = Vector3.Min(minScreen, screen);
            maxScreen = Vector3.Max(maxScreen, screen);
        }

        float width = maxScreen.x - minScreen.x;
        float height = maxScreen.y - minScreen.y;

        Debug.Log($"{gameObject.name} is ~{width:F0} x {height:F0} pixels on screen.");
    }
}
