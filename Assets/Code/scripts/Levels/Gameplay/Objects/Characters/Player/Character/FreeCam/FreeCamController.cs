using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class FreeCamController : MonoBehaviour
{
    private InputSystem_Actions inputActions;
    private Vector2 moveInput;
    private CinemachineCamera _camera;

    // Movement and Zoom Variables
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float zoomSpeed = 5f;  // Speed of Z-axis movement for zoom

    // Camera Zoom Limits (Z position limits)
    [SerializeField] private float minZDistance = -50f;  // How far back the camera can go
    [SerializeField] private float maxZDistance = 50f;   // How far forward the camera can go
    [SerializeField] float minZoomSpeedMultiplier = 1;
    [SerializeField] float maxZoomSpeedMultiplier = 2;
    float _ZoomToSpeedMultiplier;

    private void Awake()
    {
        _ZoomToSpeedMultiplier = minZoomSpeedMultiplier;
        inputActions = new InputSystem_Actions();
        inputActions.Enable();

        _camera = GetComponent<CinemachineCamera>();
        if (_camera == null)
        {
            Debug.LogError("CinemachineCamera component not found on this GameObject.");
        }
    }

    private void OnEnable()
    {
        inputActions.Camera.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Camera.Move.canceled += ctx => moveInput = Vector2.zero;

        inputActions.Camera.Zoom.performed += HandleZoom;

        inputActions.Enable();
        inputActions.Camera.Enable();
    }

    private void OnDisable()
    {
        inputActions.Camera.Zoom.performed -= HandleZoom;
        inputActions.Disable();
    }

    private void Update()
    {
        if (moveInput != Vector2.zero)
        {
            HandleMovement();
        }
    }

    private void HandleMovement()
    {
        // Move only on X and Y axes, preserving Z position for zoom control
        Vector3 movement = new Vector3(moveInput.x, moveInput.y, 0) * moveSpeed * _ZoomToSpeedMultiplier * Time.deltaTime;
        transform.position += movement;
    }

    public void HandleZoom(InputAction.CallbackContext context)
    {
        float scrollDelta = Mouse.current.scroll.ReadValue().y;

        if (Mathf.Abs(scrollDelta) > 0)
        {
            // Move camera forward/backward on Z-axis
            float zoomAmount = scrollDelta * zoomSpeed * 0.1f;
            Vector3 newPosition = transform.position + new Vector3(0, 0, zoomAmount);

            // Clamp Z position within zoom range (-50 = far out, -15 = close in)
            newPosition.z = Mathf.Clamp(newPosition.z, -50f, -15f);
            transform.position = newPosition;

            // Calculate normalized zoom ratio (0 = zoomed in, 1 = zoomed out)
            float t = Mathf.InverseLerp(-15f, -50f, newPosition.z);

            // Scale movement speed based on zoom level
            _ZoomToSpeedMultiplier = Mathf.Lerp(minZoomSpeedMultiplier, maxZoomSpeedMultiplier, t);
        }
    }
}