using UnityEngine;

/// <summary>
/// Pins the minimap camera to a fixed world position.
/// It is parented inside the HUD, so without this its view would shift whenever the HUD is moved, scaled or the screen size changes.
/// </summary>
public class MiniMapCameraAnchor : MonoBehaviour
{
    [SerializeField] Vector3 worldPosition = new Vector3(0f, 32f, -10f);

    void LateUpdate()
    {
        transform.position = worldPosition;
    }
}
