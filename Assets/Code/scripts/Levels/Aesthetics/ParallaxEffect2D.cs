using UnityEngine;

public class ParallaxEffect2D : MonoBehaviour
{
    private float startPosX, startPosY, lengthX;
    public GameObject cam;
    public float parallaxSpeedX, parallaxSpeedY;

    void Start()
    {
        startPosX = transform.position.x;
        startPosY = transform.position.y;

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        lengthX = spriteRenderer.bounds.size.x; 
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
    }
}