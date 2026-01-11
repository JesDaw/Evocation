using UnityEngine;
using System;

public class Projectile : MonoBehaviour
{
    Transform target;
    float maxMoveSpeed;
    float _maxHeight;
    AnimationCurve heightCurve, axisCurve, speedCurve;
    Action<Stats> onHitAction;
    Vector3 startPoint;
    float aliveTimer = 0f;

    public void InitializeProjectile(Transform target, float speed, float maxHeight, AnimationCurve h, AnimationCurve a, AnimationCurve s, Action<Stats> onHit)
    {
        this.target = target; 
        this.maxMoveSpeed = speed; 
        this._maxHeight = maxHeight;
        this.heightCurve = h; 
        this.axisCurve = a; 
        this.speedCurve = s;
        this.onHitAction = onHit; 
        this.startPoint = transform.position;
    }

    void Update()
    {
        aliveTimer += Time.deltaTime;
        if (target == null || aliveTimer > 10f) 
        { 
            Destroy(gameObject); 
            return; 
        }

        Vector3 range = target.position - startPoint;
        float totalDist = range.magnitude;
        float curDist = Vector3.Distance(startPoint, transform.position);
        float progress = totalDist > 0.01f ? Mathf.Clamp01(curDist / totalDist) : 1f;

        float speed = speedCurve.Evaluate(progress) * maxMoveSpeed;
        if (speed < 0.1f) speed = 0.1f;

        Vector3 nextPos = (Mathf.Abs(range.x) >= Mathf.Abs(range.y)) ? CalcX(range) : CalcY(range);
        Vector3 dir = (nextPos - transform.position).normalized;
        
        Debug.DrawLine(transform.position, nextPos, Color.red);

        transform.position += dir * speed * Time.deltaTime;

        if (dir != Vector3.zero) transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);

        if (Vector3.Distance(transform.position, target.position) < 0.5f)
        {
            if (target.TryGetComponent(out Stats s)) onHitAction?.Invoke(s);
            Destroy(gameObject);
        }
    }

    Vector3 CalcX(Vector3 r)
    {
        float nX = transform.position.x + (Mathf.Sign(r.x) * maxMoveSpeed * Time.deltaTime);
        float normX = (nX - startPoint.x) / (Mathf.Abs(r.x) < 0.01f ? 0.01f * Mathf.Sign(r.x) : r.x);
        return new Vector3(nX, startPoint.y + (heightCurve.Evaluate(normX) * _maxHeight * r.magnitude) + (axisCurve.Evaluate(normX) * r.y), 0);
    }

    Vector3 CalcY(Vector3 r)
    {
        float nY = transform.position.y + (Mathf.Sign(r.y) * maxMoveSpeed * Time.deltaTime);
        float normY = (nY - startPoint.y) / (Mathf.Abs(r.y) < 0.01f ? 0.01f * Mathf.Sign(r.y) : r.y);
        return new Vector3(startPoint.x + (heightCurve.Evaluate(normY) * _maxHeight * r.magnitude) + (axisCurve.Evaluate(normY) * r.x), nY, 0);
    }
}