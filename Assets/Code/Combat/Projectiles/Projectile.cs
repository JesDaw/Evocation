using System.Collections;
using System;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    Transform target;
    float baseSpeed;
    float maxTravelTime = 5f;
    float trajectoryMaxRelativeHeight;
    AnimationCurve heightCurve;
    AnimationCurve axisCurve;
    AnimationCurve speedCurve;
    Action<Stats> onHitAction;

    Vector3 startPoint;
    Vector3 lastKnownTargetPos;
    Vector3 moveDir;
    float aliveTimer = 0f;
    float currentProgress = 0f; // 0.0 to 1.0 along the trajectory
    float totalDistance;
    float distanceToDestroy = 0.5f;

    [SerializeField] GameObject ProjectileImpact;

    // 7-argument overload for backwards compatibility if called elsewhere
    public void InitializeProjectile(
        Transform target,
        float speed,
        float maxHeight,
        AnimationCurve h,
        AnimationCurve a,
        AnimationCurve s,
        Action<Stats> onHit)
    {
        InitializeProjectile(target, speed, 5f, maxHeight, h, a, s, onHit);
    }

    // Main 8-argument initializer
    public void InitializeProjectile(
        Transform target,
        float speed,
        float maxTravelTime,
        float maxHeight,
        AnimationCurve h,
        AnimationCurve a,
        AnimationCurve s,
        Action<Stats> onHit)
    {

        if (target == null)
        {
            Debug.LogError($"[Projectile] Target is NULL on '{gameObject.name}'. Destroying immediately.");
            Destroy(gameObject);
            return;
        }

        this.target = target;
        this.baseSpeed = speed > 0.01f ? speed : 15f;
        this.maxTravelTime = maxTravelTime > 0.01f ? maxTravelTime : 5f;

        // Ensure curves are valid even if uninitialized in the Inspector in a specific scene
        this.heightCurve = (h != null && h.length > 0) ? h : new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0));
        this.axisCurve = (a != null && a.length > 0) ? a : AnimationCurve.Linear(0, 0, 1, 1);
        this.speedCurve = (s != null && s.length > 0) ? s : AnimationCurve.Constant(0, 1, 1);

        this.onHitAction = onHit;
        this.startPoint = transform.position;
        this.lastKnownTargetPos = target.position;

        this.totalDistance = Vector3.Distance(startPoint, lastKnownTargetPos);
        float xDistanceToTarget = lastKnownTargetPos.x - startPoint.x;
        this.trajectoryMaxRelativeHeight = Mathf.Abs(xDistanceToTarget) * maxHeight;

        // Guard against missing FModAudioManager in certain scenes
        if (FModAudioManager.instance != null)
        {
            FModAudioManager.instance.PlaySoundByName("shootFireball", transform.position, 1f, 15f, "Volume", 1f);
        }
        else
        {
            Debug.LogWarning("[Projectile] FModAudioManager.instance is NULL in this scene. Skipping spawn sound.");
        }

        StartCoroutine(ProjectileCycle());
    }

    IEnumerator ProjectileCycle()
    {
        CpuStateManager targetState = target != null ? target.GetComponent<CpuStateManager>() : null;

        if (targetState != null && targetState.CurrentState == CpuStateManager.State.KnockBack)
        {
            Destroy(gameObject);
            yield break;
        }

        int frameCount = 0;

        while (true)
        {
            aliveTimer += Time.deltaTime;
            frameCount++;


            if (aliveTimer >= maxTravelTime)
            {
                Destroy(gameObject);
                yield break;
            }

            // 2. Track target position unless destroyed or in knockback
            bool targetInKnockback = targetState != null && targetState.CurrentState == CpuStateManager.State.KnockBack;
            if (target != null && !targetInKnockback)
            {
                lastKnownTargetPos = target.position;
                totalDistance = Vector3.Distance(startPoint, lastKnownTargetPos);
            }

            // 3. Advance normalized progress (0 to 1)
            float evaluatedSpeedMult = speedCurve != null ? speedCurve.Evaluate(currentProgress) : 1f;
            // Fallback if Inspector curve is flatlined at 0
            if (Mathf.Abs(evaluatedSpeedMult) < 0.01f) evaluatedSpeedMult = 1f;

            float stepDistance = baseSpeed * evaluatedSpeedMult * Time.deltaTime;
            float safeTotalDistance = Mathf.Max(totalDistance, 0.01f);
            currentProgress += stepDistance / safeTotalDistance;

            // 4. Move along curve
            UpdateProjectilePosition(Mathf.Clamp01(currentProgress));

            // 5. Check impact
            bool hitLiveTarget = (target != null && !targetInKnockback && Vector3.Distance(transform.position, target.position) <= distanceToDestroy);
            bool reachedEnd = Vector3.Distance(transform.position, lastKnownTargetPos) <= distanceToDestroy || currentProgress >= 1f;

            if (hitLiveTarget || reachedEnd)
            {
                if (hitLiveTarget && target.TryGetComponent(out Stats targetStats))
                {
                    TriggerHit(targetStats);
                }
                Destroy(gameObject);
                yield break;
            }

            yield return null;
        }
    }

    private void UpdateProjectilePosition(float t)
    {
        Vector3 trajectoryRange = lastKnownTargetPos - startPoint;

        float axisFactor = axisCurve != null ? axisCurve.Evaluate(t) : t;
        float heightOffset = (heightCurve != null ? heightCurve.Evaluate(t) : 0f) * trajectoryMaxRelativeHeight;

        Vector3 nextPosition;

        if (Mathf.Abs(trajectoryRange.normalized.x) >= Mathf.Abs(trajectoryRange.normalized.y))
        {
            // Primarily horizontal shot: curve arcs along Y
            float nextX = startPoint.x + (trajectoryRange.x * t);
            float nextY = startPoint.y + (trajectoryRange.y * axisFactor) + heightOffset;
            nextPosition = new Vector3(nextX, nextY, 0f);
        }
        else
        {
            // Primarily vertical shot: curve arcs along X
            float xOffset = heightOffset;
            if ((trajectoryRange.x > 0 && trajectoryRange.y > 0) || (trajectoryRange.x < 0 && trajectoryRange.y < 0))
            {
                xOffset = -xOffset;
            }

            float nextX = startPoint.x + (trajectoryRange.x * axisFactor) + xOffset;
            float nextY = startPoint.y + (trajectoryRange.y * t);
            nextPosition = new Vector3(nextX, nextY, 0f);
        }

        moveDir = nextPosition - transform.position;
        transform.position = nextPosition;

        if (moveDir.sqrMagnitude > 0.00001f)
        {
            float angle = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    private void TriggerHit(Stats stats)
    {
        if (ProjectileImpact != null)
        {
            GameObject explosion = Instantiate(ProjectileImpact, transform.position, Quaternion.identity);
            Destroy(explosion, 3f);
        }

        if (FModAudioManager.instance != null)
        {
            FModAudioManager.instance.PlaySoundByName("fireballHit", transform.position, 1f, 15f, "Volume", 1f);
        }

        onHitAction?.Invoke(stats);
    }

    public Vector3 GetProjectileMoveDir() => moveDir;
}