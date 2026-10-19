using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletNeedleSpell : MonoBehaviour
{
    public Vector2 direction = new Vector2(1, 0);
    public float speed = 2;

    public Vector2 velocity;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Destroy(gameObject, 3);
    }

    // Update is called once per frame
    void Update()
    {
        velocity = direction * speed;
    }

    private void FixedUpdate()
    {
        Vector2 pos = transform.position;

        pos += velocity * Time.fixedDeltaTime;

        transform.position = pos;
    }
}
