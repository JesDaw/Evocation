using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class CameraController : MonoBehaviour
{
    private InputSystem_Actions inputActions;
    private Vector2 moveInput;
    private CinemachineCamera _camera;

    // Movement and Zoom Variables
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float zoomStep = 1f;

    // Camera Zoom Limits
    [SerializeField] private float minCamSize = 5f;
    [SerializeField] private float maxCamSize = 20f;

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
        transform.position += (Vector3)(moveInput * moveSpeed * Time.deltaTime);
    }

    public void HandleZoom(InputAction.CallbackContext context)
    {
        float zoomInput = Mouse.current.scroll.ReadValue().y * 0.25f;
        if (Mathf.Abs(zoomInput) > 0)
        {
            float newSize = _camera.Lens.OrthographicSize - (zoomInput * zoomStep);
            _camera.Lens.OrthographicSize = Mathf.Clamp(newSize, minCamSize, maxCamSize);
        }
    }
}
