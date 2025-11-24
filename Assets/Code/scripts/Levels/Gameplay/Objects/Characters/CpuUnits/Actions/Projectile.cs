using UnityEngine;
using System;

public class Projectile : MonoBehaviour
{
    private Transform target;
    private AnimationCurve curve;
    private float speed;
    private float offset;
    private Action onHit;

    private Vector3 startPos;
    private float distance;
    private float t = 0;

    public void Launch(Vector3 start, Transform target, AnimationCurve curve, float speed, float offset, Action onHit)
    {
        this.startPos = start;
        this.target = target;
        this.curve = curve;
        this.speed = speed;
        this.offset = offset;
        this.onHit = onHit;

        transform.position = startPos;
        distance = Vector3.Distance(startPos, target.position);
    }

    private void FixedUpdate()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }


        t += Time.deltaTime * speed;
        float yPos = curve.Evaluate(t);

        transform.position = new Vector3(
            Mathf.Lerp(startPos.x, target.position.x, t),
            startPos.y + (yPos * distance),
            0f
        );

        float nextY = curve.Evaluate(Mathf.Clamp01(t + 0.01f));
        float angle = Mathf.Atan2(nextY - yPos, 0.01f) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle + offset);

        if (t >= 1f)
        {
            onHit?.Invoke();
            Destroy(gameObject);
        }
    }
}
