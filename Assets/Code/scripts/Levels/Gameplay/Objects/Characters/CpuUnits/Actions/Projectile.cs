using System.Collections;
using System;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    Transform target;
    float moveSpeed;
    float maxMoveSpeed;
    float trajectoryMaxRelativeHeight;
    AnimationCurve heightCurve;
    AnimationCurve axisCurve;
    AnimationCurve speedCurve;
    Action<IDamageable> onHitAction;

    Vector3 startPoint;
    Vector3 moveDir;
    float aliveTimer = 0f;
    float distanceToDestroy = 0.5f;
    [SerializeField] GameObject ProjectileImpact;

    public void InitializeProjectile(Transform target, float speed, float maxHeight,
        AnimationCurve h, AnimationCurve a, AnimationCurve s, Action<IDamageable> onHit)
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

        // Projectile sound plays here, at spawn time, regardless of which action fired it.
        FModAudioManager.instance.PlaySoundByName("shootFireball", transform.position, 1f, 15f, "Volume", 1f);

        StartCoroutine(ProjectileCycle());
    }

    IEnumerator ProjectileCycle()
    {
        Vector3 endPosition = new Vector3();
        CpuStateManager targetState = target.GetComponent<CpuStateManager>();

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

            if (!(target == null || aliveTimer > 10f) && !targetInKnockback)
            {
                endPosition = target.position;

                if (Vector3.Distance(target.position, endPosition) > 10f)
                {
                    UpdateProjectilePosition(endPosition);
                }
                else
                {
                    UpdateProjectilePosition(target.position);

                    if (Vector3.Distance(transform.position, target.position) < distanceToDestroy)
                    {
                        if (target.TryGetComponent(out Stats s)) 
                        {
                            GameObject Explosion = Instantiate(ProjectileImpact, target.position, Quaternion.identity);
                            FModAudioManager.instance.PlaySoundByName("fireballHit", transform.position, 1, 15, "Volume", 1f);
                            Destroy(Explosion, 3f);
                            onHitAction?.Invoke(s);
                        }
                        Destroy(gameObject);
                    }
                }
            }
            else
            {
                UpdateProjectilePosition(endPosition);
                if (Vector3.Distance(transform.position, endPosition) < distanceToDestroy)
                {
                    
                    Destroy(gameObject);
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
            moveSpeed = trajectoryRange.x < 0 ? -maxMoveSpeed : maxMoveSpeed;
            UpdatePositionWithYCurve(trajectoryRange);
        }
        else
        {
            moveSpeed = trajectoryRange.y < 0 ? -maxMoveSpeed : maxMoveSpeed;
            UpdatePositionWithXCurve(trajectoryRange);
        }
    }

    private void UpdatePositionWithYCurve(Vector3 trajectoryRange)
    {
        float nextPositionX = transform.position.x + moveSpeed * Time.deltaTime;
        float normalizedX = (nextPositionX - startPoint.x) / trajectoryRange.x;

        float yFromHeight = heightCurve.Evaluate(normalizedX) * trajectoryMaxRelativeHeight;
        float yAxisCorrection = axisCurve.Evaluate(normalizedX) * trajectoryRange.y;
        float nextPositionY = startPoint.y + yFromHeight + yAxisCorrection;

        Vector3 newPosition = new Vector3(nextPositionX, nextPositionY, 0);
        UpdateSpeed(normalizedX);
        moveDir = newPosition - transform.position;
        transform.position = newPosition;

        if (moveDir != Vector3.zero)
            transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg);
    }

    private void UpdatePositionWithXCurve(Vector3 trajectoryRange)
    {
        float nextPositionY = transform.position.y + moveSpeed * Time.deltaTime;
        float normalizedY = (nextPositionY - startPoint.y) / trajectoryRange.y;

        float xFromHeight = heightCurve.Evaluate(normalizedY) * trajectoryMaxRelativeHeight;

        if ((trajectoryRange.x > 0 && trajectoryRange.y > 0) ||
            (trajectoryRange.x < 0 && trajectoryRange.y < 0))
            xFromHeight = -xFromHeight;

        float xAxisCorrection = axisCurve.Evaluate(normalizedY) * trajectoryRange.x;
        float nextPositionX = startPoint.x + xFromHeight + xAxisCorrection;

        Vector3 newPosition = new Vector3(nextPositionX, nextPositionY, 0);
        UpdateSpeed(normalizedY);
        moveDir = newPosition - transform.position;
        transform.position = newPosition;

        if (moveDir != Vector3.zero)
            transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg);
    }

    private void UpdateSpeed(float normalizedProgress)
    {
        float speedMultiplier = speedCurve.Evaluate(normalizedProgress);
        moveSpeed = speedMultiplier * maxMoveSpeed;
        if (moveSpeed == 0) moveSpeed = 0.1f * Mathf.Sign(maxMoveSpeed);
    }

    public Vector3 GetProjectileMoveDir() => moveDir;
}