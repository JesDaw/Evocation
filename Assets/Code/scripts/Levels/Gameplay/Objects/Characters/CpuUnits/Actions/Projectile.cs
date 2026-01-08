using UnityEngine;
using System;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// handles projectile logic
/// </summary>
public class Projectile : MonoBehaviour
{
    private Transform target;
    private Vector3 startPos;
    private AnimationCurve curve;
    private float speed;
    private float offset;
    private Action onHit;
    private float journeyLength;
    private float distanceTraveled = 0f;

    /// <summary>
    /// Launch projectile at constant speed
    /// </summary>
    /// <param name="start">Starting position</param>
    /// <param name="targetTransform">Target to move towards</param>
    /// <param name="heightCurve">Arc curve for projectile</param>
    /// <param name="unitsPerSecond">Constant speed in units/second</param>
    /// <param name="heightOffset">Height of arc</param>
    /// <param name="onHitCallback">Callback when projectile reaches target</param>
    public void Launch(Vector3 start, Transform targetTransform, AnimationCurve heightCurve, float unitsPerSecond, float heightOffset, Action onHitCallback)
    {
        startPos = start;
        target = targetTransform;
        curve = heightCurve;
        speed = unitsPerSecond;
        offset = heightOffset;
        onHit = onHitCallback;

        transform.position = startPos;

        if (target != null)
        {
            journeyLength = Vector3.Distance(startPos, target.position);
        }
        else // Default if no target
        {
            journeyLength = 10f;
        }
    }

    void FixedUpdate()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        distanceTraveled += speed * Time.fixedDeltaTime;

        float progress = Mathf.Clamp01(distanceTraveled / journeyLength);

        Vector3 currentPos = Vector3.Lerp(startPos, target.position, progress);

        float height = curve.Evaluate(progress) * offset;
        currentPos.y += height;

        transform.position = currentPos;

        if (progress < 1f)
        {
            float nextProgress = Mathf.Clamp01((distanceTraveled + 0.1f) / journeyLength);
            Vector3 nextPos = Vector3.Lerp(startPos, target.position, nextProgress);
            float nextHeight = curve.Evaluate(nextProgress) * offset;
            nextPos.y += nextHeight;

            Vector3 direction = (nextPos - transform.position).normalized;
            if (direction != Vector3.zero)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, angle);
            }
        }

        if (progress >= 1f)
        {
            transform.position = target.position;
            onHit?.Invoke();
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        //Maybewe can make an explosion effect of something
    }
}