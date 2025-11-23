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
    [SerializeField] private float zoomSpeed = 5f;  

    [SerializeField] float minZoomSpeedMultiplier = 1;
    [SerializeField] float maxZoomSpeedMultiplier = 2;
    float _ZoomToSpeedMultiplier;

    [SerializeField] private float minZPosition = -50f;
    [SerializeField] private float maxZPosition = -15f;

 

    void Awake()
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

    void OnEnable()
    {
        inputActions.Camera.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Camera.Move.canceled += ctx => moveInput = Vector2.zero;

        inputActions.Camera.Zoom.performed += HandleZoom;

        inputActions.Enable();
        inputActions.Camera.Enable();
    }

    void Start()
    {
        GlobalInputManager.Instance.FreecamInputs = inputActions;
    }

    void OnDisable()
    {
        inputActions.Camera.Zoom.performed -= HandleZoom;
        inputActions.Disable();
    }

    void Update()
    {
        if (moveInput != Vector2.zero)
        {
            HandleMovement();
        }
    }

    void LateUpdate()
    {
        if (_camera != null)
        {
            transform.position = _camera.transform.position;
            
            UpdateZoomSpeedMultiplier();
        }
    }

    public void HandleMovement()
    {
        Vector3 movement = new Vector3(moveInput.x, moveInput.y, 0) * moveSpeed * _ZoomToSpeedMultiplier * Time.deltaTime;
        transform.position += movement;
    }

    public void HandleZoom(InputAction.CallbackContext context)
    {
        float scrollDelta = Mouse.current.scroll.ReadValue().y;

        if (Mathf.Abs(scrollDelta) > 0)
        {
            float zoomAmount = scrollDelta * zoomSpeed * 0.1f;
            Vector3 newPosition = transform.position + new Vector3(0, 0, zoomAmount);
            
            transform.position = newPosition;
        }
    }

    private void UpdateZoomSpeedMultiplier()
    {
        float t = Mathf.InverseLerp(maxZPosition, minZPosition, transform.position.z);
        _ZoomToSpeedMultiplier = Mathf.Lerp(minZoomSpeedMultiplier, maxZoomSpeedMultiplier, t);
    }
}