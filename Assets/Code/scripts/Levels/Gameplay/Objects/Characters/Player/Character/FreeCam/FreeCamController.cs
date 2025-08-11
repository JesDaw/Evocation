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

    private void Awake()
    {
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
        Vector3 movement = new Vector3(moveInput.x, moveInput.y, 0) * moveSpeed * Time.deltaTime;
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
            
            // Clamp the Z position within limits
            newPosition.z = Mathf.Clamp(newPosition.z, minZDistance, maxZDistance);
            transform.position = newPosition;
        }
    }
}