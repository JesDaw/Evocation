using UnityEngine;

public class SwitchLanes : MonoBehaviour
{
    BoxCollider2D myCollider;
    bool AbleToSwitch;
    [SerializeField] int currentLayer = 2;
    [SerializeField] GameObject[] Groundlevels;

    void Awake()
    {
        myCollider = GetComponent<BoxCollider2D>();
    }

    public float switchCooldown = 1f;
    private float lastSwitchTime = 0f;

    public void ToggleLanes()
    {
        if (Time.time - lastSwitchTime < switchCooldown) return;

        if (!AbleToSwitch) return;

        if(currentLayer >= Groundlevels.Length)
        {
            currentLayer = 0;
        }
        else currentLayer++;

        lastSwitchTime = Time.time;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("swiched layer from" + collision.gameObject.layer);

        if (collision.gameObject.layer == 9 || collision.gameObject.layer == 18 || collision.gameObject.layer == 19 || collision.gameObject.layer == 20)
        {
            if(currentLayer == 0) collision.gameObject.layer = 18;
            if(currentLayer == 1) collision.gameObject.layer = 19;
            if(currentLayer == 2) collision.gameObject.layer = 20;
        }

        if (collision.gameObject.layer == 10 || collision.gameObject.layer == 15 || collision.gameObject.layer == 16 || collision.gameObject.layer == 17)
        {
            if(currentLayer == 0) collision.gameObject.layer = 15;
            if(currentLayer == 1) collision.gameObject.layer = 16;
            if(currentLayer == 2) collision.gameObject.layer = 17;
        }
        Debug.Log("layer is now"+collision.gameObject.layer);
    }

}
