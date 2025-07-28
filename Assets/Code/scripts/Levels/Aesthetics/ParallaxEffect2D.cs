using UnityEngine;

public class ParallaxEffect2D : MonoBehaviour
{
    private float startPosX, startPosY, lengthX;
    public GameObject cam;
    public float parallaxSpeedX, parallaxSpeedY;

    private float originalOrthoSize;
    private Vector3 originalScale;

    [Range(0f, 1f)] [SerializeField] float zoomInfluenceFactor = 1f;


    void Start()
    {
        startPosX = transform.position.x;
        startPosY = transform.position.y;

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        lengthX = spriteRenderer.bounds.size.x;

        originalOrthoSize = cam.GetComponent<Camera>().orthographicSize;
        originalScale = transform.localScale;
    }

    void LateUpdate()
    {
        
        float distanceX = cam.transform.position.x * parallaxSpeedX;
        float distanceY = cam.transform.position.y * parallaxSpeedY;
        transform.position = new Vector3(startPosX + distanceX, startPosY + distanceY, transform.position.z);

        
        if (Mathf.Abs(cam.transform.position.x - transform.position.x) >= lengthX)
        {
            startPosX += Mathf.Sign(cam.transform.position.x - transform.position.x) * lengthX;
        }

        
        float currentOrthoSize = cam.GetComponent<Camera>().orthographicSize;
        float zoomRatio = currentOrthoSize / originalOrthoSize;

        
        float adjustedZoomRatio = Mathf.Lerp(1f, zoomRatio, zoomInfluenceFactor);
        transform.localScale = originalScale * adjustedZoomRatio;
    }
}
