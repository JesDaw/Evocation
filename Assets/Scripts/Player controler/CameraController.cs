using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField]
    private Camera cam;
    private InputSystem_Actions inputActions; //reference to input sys

    [SerializeField]
    public float moveSpeed = 10f;
    public float zoomStep = 1f;

    [SerializeField]
    private SpriteRenderer mapRenderer;
    private float mapMinX, mapMaxX, mapMinY, mapMaxY;

    private PlayerInput playerInput; //using unity input sys
    private Vector2 moveInput; //WASD
    private float zoomInput; //mouse scroll wheel
    private float minCamSize, maxCamSize;


    private void Awake()
    {
        //playerInput = GetComponent<PlayerInput>(); //GetComponent gets the PlayerInput attached to game object (the backgroudn)
        inputActions = new InputSystem_Actions(); //initialize InputSystem_Actions(input actions editor)
        inputActions.Enable();  //enable the input actions

        //removed map bounds calculations; it is now done dynamically
        mapMinX = mapRenderer.bounds.min.x;
        mapMaxX = mapRenderer.bounds.max.x;
        mapMinY = mapRenderer.bounds.min.y;
        mapMaxY = mapRenderer.bounds.max.y;

        //fixed the zoom in/out limits
        maxCamSize = mapRenderer.bounds.size.y / 2f;
        minCamSize = maxCamSize / 4f;

        cam.orthographicSize = maxCamSize;

    }

    private void OnEnable()
    {
        inputActions.Camera.Move.performed += MoveCamera;  //move binding
        //inputActions.Camera.Zoom.performed += Zoom;  //zoom binding
        if (inputActions == null)
        {
            inputActions = new InputSystem_Actions();
        }

    inputActions.Enable();
    inputActions.Camera.Enable();
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    // Update is called once per frame
    private void Update()
    {
        //MoveCamera();
        //continuous movement input; holding down on WASD

        Vector2 move = inputActions.Camera.Move.ReadValue<Vector2>();

        Vector3 newPosition = cam.transform.position + new Vector3(move.x, move.y, 0) * moveSpeed * Time.deltaTime;
        cam.transform.position = ClampCamera(newPosition);

        if (inputActions.Camera.Zoom != null)
        {
            zoomInput = Mouse.current.scroll.ReadValue().y * 0.25f; // read zoom input
            if (zoomInput != 0)  
            {
                Zoom();  
            }
        }
    }

    private void MoveCamera(InputAction.CallbackContext ctx)
    {
        Vector2 move = ctx.ReadValue<Vector2>(); //read move input; WASD
        Vector3 newPosition = cam.transform.position + new Vector3(move.x, move.y, 0) * moveSpeed * Time.deltaTime;
        cam.transform.position = ClampCamera(newPosition);
    }

    private void Zoom()
    {
        //float zoomInput = ctx.ReadValue<float>(); //read zoom input; scroll wheel
        //float newSize = cam.orthographicSize - zoomInput * zoomStep;
        
        //scroll up = zoom in; decrease orthographic size
        //scroll down = zoom out; increase orthographic size
        float zoomChange = zoomInput * zoomStep;
        float newSize = cam.orthographicSize - zoomChange;
        
        cam.orthographicSize = Mathf.Clamp(newSize, minCamSize, maxCamSize);
        cam.transform.position = ClampCamera(cam.transform.position);
    }

    private Vector3 ClampCamera(Vector3 targetPosition)
    {
        float camHeight = cam.orthographicSize;
        float camWidth = cam.orthographicSize * cam.aspect;

        //adjust to the actual size of the map
        float map_width = mapRenderer.bounds.size.x;
        float map_height = mapRenderer.bounds.size.y;

        float minX = mapMinX + camWidth;
        float maxX = mapMaxX - camWidth;
        float minY = mapMinY + camHeight;
        float maxY = mapMaxY - camHeight;

        float newX = Mathf.Clamp(targetPosition.x, minX, maxX);
        float newY = Mathf.Clamp(targetPosition.y, minY, maxY);

        return new Vector3(newX, newY, targetPosition.z);
    }
}
