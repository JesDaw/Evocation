using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    // 🎥 Camera Reference
    [SerializeField] private Camera cam;

    // 🎮 Input System Actions
    private InputSystem_Actions inputActions;

    // ⚡ Movement and Zoom Variables
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float zoomStep = 1f;

    // 📍 Map Boundaries
    [SerializeField] private SpriteRenderer mapRenderer;
    private float mapMinX, mapMaxX, mapMinY, mapMaxY;
    
    // 📌 Camera Zoom Limits
    private float minCamSize, maxCamSize;

    private void Awake()
    {
        inputActions = new InputSystem_Actions();
        inputActions.Enable();

        if (mapRenderer != null)
        {
            mapMinX = mapRenderer.bounds.min.x;
            mapMaxX = mapRenderer.bounds.max.x;
            mapMinY = mapRenderer.bounds.min.y;
            mapMaxY = mapRenderer.bounds.max.y;

            maxCamSize = mapRenderer.bounds.size.y / 2f;
            minCamSize = maxCamSize / 4f;

            cam.orthographicSize = maxCamSize;
        }
        else
        {
            Debug.LogError("Map Renderer is not assigned!");
        }
    }

    private void OnEnable()
    {
        inputActions.Camera.Move.performed += OnMoveCamera;
        inputActions.Enable();
        inputActions.Camera.Enable();
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    private void Update()
    {
        HandleMovement();
        HandleZoom();
    }

    /// <summary>
    /// Handles continuous movement based on input.
    /// </summary>
    private void HandleMovement()
    {
        Vector2 moveInput = inputActions.Camera.Move.ReadValue<Vector2>();
        Vector3 newPosition = cam.transform.position + new Vector3(moveInput.x, moveInput.y, 0) * moveSpeed * Time.deltaTime;
        cam.transform.position = ClampCamera(newPosition);
    }

    /// <summary>
    /// Handles zooming in and out based on mouse scroll input.
    /// </summary>
    private void HandleZoom()
    {
        float zoomInput = Mouse.current.scroll.ReadValue().y * 0.25f; 
        if (Mathf.Abs(zoomInput) > 0)
        {
            float newSize = cam.orthographicSize - (zoomInput * zoomStep);
            cam.orthographicSize = Mathf.Clamp(newSize, minCamSize, maxCamSize);
            cam.transform.position = ClampCamera(cam.transform.position);
        }
    }

    /// <summary>
    /// Moves the camera based on input.
    /// </summary>
    private void OnMoveCamera(InputAction.CallbackContext ctx)
    {
        Vector2 move = ctx.ReadValue<Vector2>();
        Vector3 newPosition = cam.transform.position + new Vector3(move.x, move.y, 0) * moveSpeed * Time.deltaTime;
        cam.transform.position = ClampCamera(newPosition);
    }

    /// <summary>
    /// Ensures the camera stays within the map bounds.
    /// </summary>
    private Vector3 ClampCamera(Vector3 targetPosition)
    {
        if (mapRenderer == null) return targetPosition;

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
