using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class CameraController : MonoBehaviour
{
    // 🎮 Input System Actions
    private InputSystem_Actions inputActions;
    private Rigidbody2D rb;
    private Vector2 moveInput; // Stores movement input

    // 🎥 Camera Reference
    private CinemachineCamera _camera;

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
        _camera = GetComponent<CinemachineCamera>();
        rb = GetComponent<Rigidbody2D>();

        if (rb == null)
        {
            Debug.LogError("Rigidbody2D is missing on Camera!");
        }

        if (mapRenderer != null)
        {
            mapMinX = mapRenderer.bounds.min.x;
            mapMaxX = mapRenderer.bounds.max.x;
            mapMinY = mapRenderer.bounds.min.y;
            mapMaxY = mapRenderer.bounds.max.y;

            maxCamSize = mapRenderer.bounds.size.y / 2f;
            minCamSize = maxCamSize / 4f;

            _camera.Lens.OrthographicSize = maxCamSize;
        }
        else
        {
            Debug.LogError("Map Renderer is not assigned!");
        }
    }

    private void OnEnable()
    {
        inputActions.Camera.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Camera.Move.canceled += ctx => moveInput = Vector2.zero; // Stop movement when key is released

        // Bind HandleZoom to zoom action
        inputActions.Camera.Zoom.performed += HandleZoom;

        inputActions.Enable();
        inputActions.Camera.Enable();
    }

    private void OnDisable()
    {
        inputActions.Camera.Zoom.performed -= HandleZoom; // Unbind the zoom action when disabled
        inputActions.Disable();
    }

    private void Update()
    {
        if (moveInput != Vector2.zero)
        {
            HandleMovement();
        }
    }

    /// <summary>
    /// Handles continuous movement based on input.
    /// </summary>
    private void HandleMovement()
    {
        Vector3 newPosition = transform.position + new Vector3(moveInput.x, moveInput.y, 0) * moveSpeed * Time.deltaTime;
        transform.position = ClampCamera(newPosition);
    }

    /// <summary>
    /// Handles zooming in and out based on mouse scroll input.
    /// </summary>
    public void HandleZoom(InputAction.CallbackContext context)
    {
        // Zoom input from mouse scroll (y-axis)
        float zoomInput = Mouse.current.scroll.ReadValue().y * 0.25f;
        if (Mathf.Abs(zoomInput) > 0)
        {
            // Calculate new zoom size
            float newSize = _camera.Lens.OrthographicSize - (zoomInput * zoomStep);
            // Clamp the zoom size within min and max range
            _camera.Lens.OrthographicSize = Mathf.Clamp(newSize, minCamSize, maxCamSize);
            // Clamp camera position to prevent it from going outside map boundaries
            _camera.transform.position = ClampCamera(_camera.transform.position);
        }
    }

    /// <summary>
    /// Ensures the camera stays within the map bounds.
    /// </summary>
    private Vector3 ClampCamera(Vector3 targetPosition)
    {
        if (mapRenderer == null) return targetPosition;

        float camHeight = _camera.Lens.OrthographicSize;
        float camWidth = _camera.Lens.OrthographicSize * _camera.Lens.Aspect;

        float minX = mapMinX + camWidth;
        float maxX = mapMaxX - camWidth;
        float minY = mapMinY + camHeight;
        float maxY = mapMaxY - camHeight;

        float newX = Mathf.Clamp(targetPosition.x, minX, maxX);
        float newY = Mathf.Clamp(targetPosition.y, minY, maxY);

        return new Vector3(newX, newY, targetPosition.z);
    }
}
