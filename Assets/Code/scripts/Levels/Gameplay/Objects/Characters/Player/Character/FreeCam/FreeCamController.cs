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
    [SerializeField] private float zoomStep = 5f;  // Bigger step size for FOV feels better

    // Camera Zoom Limits (FOV for perspective camera)
    [SerializeField] private float minFOV = 40f;
    [SerializeField] private float maxFOV = 80f;

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
        float scrollDelta = Mouse.current.scroll.ReadValue().y;

        if (Mathf.Abs(scrollDelta) > 0)
        {
            float newFOV = _camera.Lens.FieldOfView - (scrollDelta * zoomStep * 0.1f);  // scroll up = zoom in
            _camera.Lens.FieldOfView = Mathf.Clamp(newFOV, minFOV, maxFOV);
        }
    }
}
