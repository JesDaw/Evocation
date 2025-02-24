using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
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

    private PlayerInput playerInput; //using unity input sys
    private Vector2 moveInput;
    private float zoomInput;

    private void Awake()
    {
        playerInput = new PlayerInput();
        playerInput.Camera.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        playerInput.Camera.Move.canceled += ctx => moveInput = Vector2.zero;
        playerInput.Camera.Zoom.performed += ctx => zoomInput = ctx.ReadValue<float>();
        playerInput.Camera.Zoom.canceled += ctx => zoomInput = 0f;

        mapMinX = mapRenderer.transform.position.x - mapRenderer.bounds.size.x / 2f;
        mapMaxX = mapRenderer.transform.position.x + mapRenderer.bounds.size.x / 2f;

        mapMinY = mapRenderer.transform.position.y - mapRenderer.bounds.size.y / 2f;
        mapMaxY = mapRenderer.transform.position.y + mapRenderer.bounds.size.y / 2f;
    }

    private void OnEnable() => playerInput.Enable();
    private void OnDisable() => playerInput.Disable();

    // Update is called once per frame
    void Update()
    {
        MoveCamera();
        Zoom();
    }

    private void MoveCamera()
    {
        Vector3 moveVector = new Vector3(moveInput.x, moveInput.y, 0) * moveSpeed * Time.deltaTime;
        cam.transform.position = ClampCamera(cam.transform.position + moveVector);
    }

    private void Zoom()
    {
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
