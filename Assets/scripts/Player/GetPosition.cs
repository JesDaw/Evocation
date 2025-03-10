using UnityEngine;

public class GetPosition : MonoBehaviour
{
    public GameObject myObject;

    public Vector3 getPosition()
    {
        return myObject.transform.position;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
