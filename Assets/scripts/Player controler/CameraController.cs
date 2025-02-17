using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField]
    private Camera cam;

    [SerializeField]
    private float moveSpeed = 10f;
    private float zoomStep = 1f, minCamSize = 2f, maxCamSize = 6.4f;

    [SerializeField]
    private SpriteRenderer mapRenderer;
    private float mapMinX, mapMaxX, mapMinY, mapMaxY;


    private void Awake()
    {
        mapMinX = mapRenderer.transform.position.x - mapRenderer.bounds.size.x / 2f;
        mapMaxX = mapRenderer.transform.position.x + mapRenderer.bounds.size.x / 2f;

        mapMinY = mapRenderer.transform.position.y - mapRenderer.bounds.size.y / 2f;
        mapMaxY = mapRenderer.transform.position.y + mapRenderer.bounds.size.y / 2f;
    }

    // Update is called once per frame
    void Update()
    {
        MoveCamera();
        Zoom();
    }

    private void MoveCamera()
    {
        float moveX = Input.GetAxis("Horizontal") * moveSpeed * Time.deltaTime; //A/D right and left
        float moveY = Input.GetAxis("Vertical") * moveSpeed * Time.deltaTime; //W/D up and down

        Vector3 newPosition = cam.transform.position + new Vector3(moveX, moveY, 0);
        cam.transform.position = ClampCamera(newPosition);
    }

    private void Zoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll != 0)
        {
            float newSize = cam.orthographicSize - scroll * zoomStep;
            cam.orthographicSize = Mathf.Clamp(newSize, minCamSize, maxCamSize);
            cam.transform.position = ClampCamera(cam.transform.position);
        }
    }

    private Vector3 ClampCamera(Vector3 targetPosition)
    {
        float camHeight = cam.orthographicSize;
        float camWidth = cam.orthographicSize * cam.aspect;

        float minX = mapMinX + camWidth;
        float maxX = mapMaxX - camWidth;
        float minY = mapMinY + camHeight;
        float maxY = mapMaxY - camHeight;

        float newX = Mathf.Clamp(targetPosition.x, minX, maxX);
        float newY = Mathf.Clamp(targetPosition.y, minY, maxY);

        return new Vector3(newX, newY, targetPosition.z);
    }
}
