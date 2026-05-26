using UnityEngine;
using System.Collections;
using System;

public class Projectile : MonoBehaviour
{
    Transform target;
    float moveSpeed;
    float maxMoveSpeed;
    float trajectoryMaxRelativeHeight;
    AnimationCurve heightCurve; // determins the arc of the projectile
    AnimationCurve axisCurve; // helps for instances when the target is above or below the shooter
    AnimationCurve speedCurve; // speed of the projectile over time
    Action<IDamageable> onHitAction;
    
    Vector3 startPoint;
    Vector3 moveDir;
    float aliveTimer = 0f;
    float distanceToDestroy = 0.5f;
    [SerializeField] GameObject ProjectileImpact;

    public void InitializeProjectile(Transform target, float speed, float maxHeight, AnimationCurve h, AnimationCurve a, AnimationCurve s,
                                     Action<IDamageable> onHit)
    {
        this.target = target;
        this.maxMoveSpeed = speed;
        this.heightCurve = h;
        this.axisCurve = a;
        this.speedCurve = s;
        this.onHitAction = onHit;
        this.startPoint = transform.position;
        
        float xDistanceToTarget = target.position.x - startPoint.x;
        this.trajectoryMaxRelativeHeight = Mathf.Abs(xDistanceToTarget) * maxHeight;
		StartCoroutine(ProjectileCycle());
    }

	IEnumerator ProjectileCycle()
    {
        Vector3 endPosition = target.position;
        CpuStateManager targetState = target.GetComponent<CpuStateManager>(); // may be null for player

        // Only bail early on knockback if the component actually exists
        if (targetState != null && targetState.CurrentState == CpuStateManager.State.KnockBack)
        {
            Destroy(gameObject);
            yield break;
        }

        while (true)
        {
            aliveTimer += Time.deltaTime;

            bool targetInKnockback = targetState != null && 
                                    targetState.CurrentState == CpuStateManager.State.KnockBack;

            if (target != null && aliveTimer <= 10f && !targetInKnockback)
            {
                endPosition = target.position;

                UpdateProjectilePosition(target.position);

                if (Vector3.Distance(transform.position, target.position) < distanceToDestroy)
                {
                    if (target.TryGetComponent(out Stats s))
                    {
                        try
                        {
                            onHitAction?.Invoke(s);
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogError($"[Projectile] onHitAction threw on target '{s.gameObject.name}': {e}");
                        }
                    }
                    FModAudioManager.instance.PlaySoundByName("fireballHit", transform.position, 1, 15, "Volume", 1f);
                    GameObject Explosion = Instantiate(ProjectileImpact, target.position, Quaternion.identity);
                    Destroy(Explosion, 3f);
                    Destroy(gameObject);
                    yield break;
                }
            }
            else
            {
                UpdateProjectilePosition(endPosition);
                if (Vector3.Distance(transform.position, endPosition) < distanceToDestroy)
                {
                    Destroy(gameObject);
                    yield break;
                }
            }

            yield return null;
        }
    }

    private void UpdateProjectilePosition(Vector3 _targetPoint)
    {
        Vector3 trajectoryRange = _targetPoint - startPoint;

        if (Mathf.Abs(trajectoryRange.normalized.x) >= Mathf.Abs(trajectoryRange.normalized.y))
        {
            if (trajectoryRange.x < 0) // for when the target is on the left side
            {
                moveSpeed = -maxMoveSpeed;
            }
            else
            {
                moveSpeed = maxMoveSpeed;
            }
            UpdatePositionWithYCurve(trajectoryRange);
        }
        else
        {
            if (trajectoryRange.y < 0)
            {
                moveSpeed = -maxMoveSpeed;
            }
            else
            {
                moveSpeed = maxMoveSpeed;
            }
            UpdatePositionWithXCurve(trajectoryRange);
        }
    }

    private void UpdatePositionWithYCurve(Vector3 trajectoryRange)
    {
        float nextPositionX = transform.position.x + moveSpeed * Time.deltaTime;
        float normalizedX = (nextPositionX - startPoint.x) / trajectoryRange.x;

        float heightValue = heightCurve.Evaluate(normalizedX);
        float yFromHeight = heightValue * trajectoryMaxRelativeHeight;

        float axisCorrectionValue = axisCurve.Evaluate(normalizedX);
        float yAxisCorrection = axisCorrectionValue * trajectoryRange.y;

        float nextPositionY = startPoint.y + yFromHeight + yAxisCorrection;

        Vector3 newPosition = new Vector3(nextPositionX, nextPositionY, 0);

        UpdateSpeed(normalizedX);
        
        moveDir = newPosition - transform.position;

        transform.position = newPosition;

        if (moveDir != Vector3.zero)
        {
            transform.rotation = Quaternion.Euler(0, 0, 
                Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg);
        }
    }

    private void UpdatePositionWithXCurve(Vector3 trajectoryRange)
    {
        float nextPositionY = transform.position.y + moveSpeed * Time.deltaTime;
        float normalizedY = (nextPositionY - startPoint.y) / trajectoryRange.y;

        float heightValue = heightCurve.Evaluate(normalizedY);
        float xFromHeight = heightValue * trajectoryMaxRelativeHeight;

        if (trajectoryRange.x > 0 && trajectoryRange.y > 0)
        {
            xFromHeight = -xFromHeight;
        }
        if (trajectoryRange.x < 0 && trajectoryRange.y < 0)
        {
            xFromHeight = -xFromHeight;
        }

        float axisCorrectionValue = axisCurve.Evaluate(normalizedY);
        float xAxisCorrection = axisCorrectionValue * trajectoryRange.x;

        float nextPositionX = startPoint.x + xFromHeight + xAxisCorrection;

        Vector3 newPosition = new Vector3(nextPositionX, nextPositionY, 0);

        UpdateSpeed(normalizedY);
        
        moveDir = newPosition - transform.position;

        transform.position = newPosition;

        if (moveDir != Vector3.zero)
        {
            transform.rotation = Quaternion.Euler(0, 0, 
                Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg);
        }
    }

    private void UpdateSpeed(float normalizedProgress)
    {
        float speedMultiplier = speedCurve.Evaluate(normalizedProgress);
        moveSpeed = speedMultiplier * maxMoveSpeed;
        
        if (moveSpeed == 0) moveSpeed = 0.1f * Mathf.Sign(maxMoveSpeed);
    }

    public Vector3 GetProjectileMoveDir()
    {
        return moveDir;
    }
}
