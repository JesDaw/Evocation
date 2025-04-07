using UnityEngine;

public class ProjectileScript : MonoBehaviour
{
    [SerializeField] SpriteRenderer Renderer;
    public void UpdateProjectile(Vector3 _Start, GameObject _EndEnemy, ScrProjectiles _Projectile)
    {
        Debug.Log("Updated Projectles");
        Projectiles = _Projectile;
        Start = _Start;
        EnemyObject = _EndEnemy;
        ProCurve = _Projectile._TrajectoryCurve;
        Speed = _Projectile._Speed;

        Renderer.sprite = _Projectile._Appearance; 
    }

    Vector3 Start; 
    GameObject EnemyObject;
    ScrProjectiles Projectiles;
    AnimationCurve ProCurve;
    float Speed;
    Stats DetectedStats;

    void FixedUpdate()
    {
        float t = Time.time * Speed % 1;
        float yPos = ProCurve.Evaluate(t);
        transform.position = new Vector3(
            Start.x + (t * Vector3.Distance(Start, EnemyObject.transform.position)),
            Start.y + (yPos * Vector3.Distance(Start, EnemyObject.transform.position)),
            0);

        Vector3 rotation = transform.rotation.eulerAngles;

        float currentAngle = Mathf.Atan2(yPos, ProCurve.Evaluate(t + 0.01f)) * Mathf.Rad2Deg;
        float targetAngle = currentAngle + Projectiles._Offset;

        rotation.z = Mathf.LerpAngle(rotation.z, targetAngle, Time.deltaTime * 10);

        transform.rotation = Quaternion.Euler(rotation);

        if (Mathf.Approximately(t, 1.0f) || t > 0.99f)
        {
            OnEndReached();
        }

    }

    void OnEndReached()
    {
        Debug.Log("End reached! Do something here.");
        DetectedStats = EnemyObject?.GetComponent<Stats>();
        if (DetectedStats == null) return;

        DetectedStats.Attack(Projectiles._Damage);

        Destroy(this.gameObject);
    }

}
