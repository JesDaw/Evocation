using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class FreeCamController : MonoBehaviour
{
    private Vector2 moveInput;
    private CinemachineCamera _camera;

    // Movement and Zoom Variables
    [SerializeField] float moveSpeed = 10f;
    [SerializeField] float zoomSpeed = 5f;  

    [SerializeField] float maxZoomSpeedMultiplier = 2;
    float minZoomSpeedMultiplier = 1;
    float _ZoomToSpeedMultiplier;

    [SerializeField] GameObject CameraConfiner;
    [SerializeField] bool DebugLogs = false;
    Bounds confineBounds;
    float minZPosition;
    float maxZPosition;
    public static FreeCamController Instance { get; private set; }


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _ZoomToSpeedMultiplier = minZoomSpeedMultiplier;

        _camera = GetComponent<CinemachineCamera>();
        if (_camera == null)
        {
            Debug.LogError($"CinemachineCamera component not found on {gameObject.name}.");
        }

        CalculateBounds();
    }

    void Start()
    {
        // Subscribe to input from the GlobalInputManager (with safety check)
        if (GlobalInputManager.Instance != null)
        {
            SubscribeToInputs();
            if(DebugLogs)Debug.Log("[FreeCamController] Subscribed to camera inputs");
        }
        else
        {
            Debug.LogError("[FreeCamController] GlobalInputManager instance null - cannot subscribe");
        }
    }

    void OnDisable()
    {
        // Unsubscribe when disabled
        UnsubscribeFromInputs();
    }

    void SubscribeToInputs()
    {
        if (GlobalInputManager.Instance == null) return;

        var cameraActions = GlobalInputManager.Instance.InputActions.Camera;
        
        cameraActions.Move.performed += OnMove;
        cameraActions.Move.canceled += OnMove;
        cameraActions.Zoom.performed += HandleZoom;
    }

    void UnsubscribeFromInputs()
    {
        if (GlobalInputManager.Instance == null) return;

        var cameraActions = GlobalInputManager.Instance.InputActions.Camera;
        
        cameraActions.Move.performed -= OnMove;
        cameraActions.Move.canceled -= OnMove;
        cameraActions.Zoom.performed -= HandleZoom;
    }

    void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    void CalculateBounds()
    {
        if (CameraConfiner == null)
        {
            Debug.LogError("CameraConfiner is not assigned to the freecam controller script");
            return;
        }

        BoxCollider boxCollider = CameraConfiner.GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            Debug.LogError("CameraConfiner doesn't have a BoxCollider on it");
            return;
        }

        // Store the bounds for clamping
        confineBounds = boxCollider.bounds;
        
        // Calculate the Z extents for zoom speed multiplier
        float zMin = confineBounds.center.z - confineBounds.extents.z;
        float zMax = confineBounds.center.z + confineBounds.extents.z;

        // Closest to z = 0 is max (less zoomed out), furthest is min (more zoomed out)
        if (Mathf.Abs(zMin) < Mathf.Abs(zMax))
        {
            maxZPosition = zMin;
            minZPosition = zMax;
        }
        else
        {
            maxZPosition = zMax;
            minZPosition = zMin;
        }
    }

    void Update()
    {
        if (moveInput != Vector2.zero)
        {
            HandleMovement();
        }
        
        UpdateZoomSpeedMultiplier();
    }

    public void HandleMovement()
    {
        if (DebugLogs) Debug.Log($"[FreeCamController] Handling movement: {moveInput}, speed: {moveSpeed * _ZoomToSpeedMultiplier}");
        Vector3 movement = new Vector3(moveInput.x, moveInput.y, 0) * moveSpeed * _ZoomToSpeedMultiplier * Time.deltaTime;
        Vector3 newPosition = transform.position + movement;
        
        // Clamp to bounds
        newPosition.x = Mathf.Clamp(newPosition.x, confineBounds.min.x, confineBounds.max.x);
        newPosition.y = Mathf.Clamp(newPosition.y, confineBounds.min.y, confineBounds.max.y);
        newPosition.z = Mathf.Clamp(newPosition.z, confineBounds.min.z, confineBounds.max.z);
        
        transform.position = newPosition;
    }

    public void HandleZoom(InputAction.CallbackContext context)
    {
        float scrollDelta = Mouse.current.scroll.ReadValue().y;

        if (Mathf.Abs(scrollDelta) > 0)
        {
            float zoomAmount = scrollDelta * zoomSpeed * 0.1f;
            Vector3 newPosition = transform.position + new Vector3(0, 0, zoomAmount);
            
            // Clamp to bounds
            newPosition.x = Mathf.Clamp(newPosition.x, confineBounds.min.x, confineBounds.max.x);
            newPosition.y = Mathf.Clamp(newPosition.y, confineBounds.min.y, confineBounds.max.y);
            newPosition.z = Mathf.Clamp(newPosition.z, confineBounds.min.z, confineBounds.max.z);
            
            transform.position = newPosition;
        }
    }

    void UpdateZoomSpeedMultiplier()
    {
        float t = Mathf.InverseLerp(maxZPosition, minZPosition, transform.position.z);
        _ZoomToSpeedMultiplier = Mathf.Lerp(minZoomSpeedMultiplier, maxZoomSpeedMultiplier, t);
    }   
}