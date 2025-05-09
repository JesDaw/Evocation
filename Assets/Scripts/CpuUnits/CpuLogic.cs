using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CpuLogic : MonoBehaviour
{
    public ScriptableStats ScrStats;
    public bool _Enemy; 

    [SerializeField] Stats _Stats;

    [Header("Required Components")]
    [SerializeField] Transform _Raycast;
    [SerializeField] SpriteRenderer _Renderer;
    [SerializeField] Rigidbody2D _Body;
    [SerializeField] AudioSource attackingAudio;
    [SerializeField] Animator animator;

    [SerializeField] float _RadiusDetection;

    [Header("Events")]
    [SerializeField] UnityEvent OnSpawn;
    [SerializeField] CpuUtilis Utilis;

    private bool _AlreadyAttacked = false;
    private bool _Freeze = false;

    void Start()
    {
        // Copy stats from ScriptableObject
        _Stats._Clan = ScrStats._Clan;
        gameObject.tag = _Stats._Clan;
        _Stats._Health = ScrStats._Health;
        _Stats._AttackDamage = ScrStats._AttackDamage;
        _Stats._AttackStartup = ScrStats._AttackStartup;
        _Stats._AttackActiveDuration = ScrStats._AttackActiveDuration;
        _Stats._AttackEndlag = ScrStats._AttackEndlag;
        _Stats._MoveSpeed = ScrStats._MoveSpeed;
        _Stats._StopDistance = ScrStats._StopDistance + Random.Range(-0.3f, 0.3f);
        _Stats._CpuPriority = ScrStats._CpuPriority;
        _Stats._KnockBackHealth = ScrStats._KnockBackHealth;
        _Stats._KnockBackVelocity = ScrStats._KnockBackVelocity;
        _Stats._KnockBackMax = ScrStats._KnockBackHealth;
        _Stats._StatusHealth = ScrStats._StatusHealth;
        _Stats._StatusMax = ScrStats._StatusHealth;

        _Renderer.sprite = ScrStats._Sprite;
        OnSpawn.Invoke();
    }

    void Update()
    {
        if (_Freeze) return;

        RaycastHit2D[] hits = Physics2D.RaycastAll(_Raycast.position, transform.right, _Stats._StopDistance);
        Debug.DrawRay(_Raycast.position, transform.right * _Stats._StopDistance, Color.red);

        Collider2D[] surroundingHits = Physics2D.OverlapCircleAll(transform.position, _RadiusDetection, LayerMask.GetMask("Player"));
        if (surroundingHits.Length == 0)
        {
            SwitchSides(false);
        }
        else
        {
            bool faceRight = surroundingHits[0].transform.position.x - transform.position.x > 0;
            if (_Enemy) faceRight = !faceRight;
            SwitchSides(faceRight);
        }

        _Stats._MoveSpeed = ScrStats._MoveSpeed;
        if (hits.Length == 0) return;

        GameObject target = FindTargetFromRaycast(hits);
        if (target == null) return;

        _Stats._MoveSpeed = 0;

        if (!_AlreadyAttacked)
        {
            StartCoroutine(AttackRoutine(target));
        }
    }

    GameObject FindTargetFromRaycast(RaycastHit2D[] hits)
    {
        for (int i = 0; i < _Stats._CpuPriority.Count; i++)
        {
            foreach (RaycastHit2D hit in hits)
            {
                if (gameObject.layer == 10 && hit.collider.gameObject.layer == 11) continue;
                if (hit.collider.gameObject.layer == gameObject.layer) continue;
                if (hit.collider.CompareTag(_Stats._CpuPriority[i]))
                {
                    return hit.collider.gameObject;
                }
            }
        }
        return null;
    }

    IEnumerator AttackRoutine(GameObject target)
    {
        _AlreadyAttacked = true;
        _Stats._MoveSpeed = 0;
        _Freeze = true;

        yield return new WaitForSeconds(_Stats._AttackStartup / 60f);

        if (animator != null)
            animator.SetTrigger("Attack");

        if (target != null)
        {
            if (target.TryGetComponent(out Stats enemyStats))
            {
                enemyStats.Attack(_Stats._AttackDamage);

                foreach (var effect in ScrStats.OnAttack)
                    Utilis.SelectOnAttack(effect, ScrStats, target);

                if (enemyStats._StatusHealth <= 0)
                {
                    foreach (StatusEffect effect in ScrStats._EffectsToApply)
                    {
                        if (effect != null)
                            enemyStats.AddStatusEffect(effect);
                    }
                }
                else
                {
                    enemyStats._StatusHealth--;
                }
            }
            else if (target.TryGetComponent(out BuildingHealth buildingHealth))
            {
                buildingHealth.TakeDamage(_Stats._AttackDamage);
            }
        }

        attackingAudio.Play();
        yield return new WaitForSeconds(_Stats._AttackActiveDuration / 60f);
        yield return new WaitForSeconds(_Stats._AttackEndlag / 60f);

        _Freeze = false;
        _AlreadyAttacked = false;
    }

    void SwitchSides(bool faceRight)
    {
        if (_Enemy) faceRight = !faceRight;

        transform.eulerAngles = faceRight ? new Vector3(0, 180, 0) : Vector3.zero;

        if (transform.childCount > 0)
        {
            transform.GetChild(0).localRotation = Quaternion.identity;
        }
    }

    public void ApplyTempSpeed(Vector2 speedInfo)
    {
        StartCoroutine(TempSpeed(speedInfo.x));
    }

    IEnumerator TempSpeed(float tempSpeed)
    {
        _Freeze = true;
        _Stats._MoveSpeed = tempSpeed;
        yield return new WaitForSeconds(ScrStats._MoveSpeed / 6f);
        _Freeze = false;
    }

    void FixedUpdate()
    {
        _Body.linearVelocity = new Vector2(_Stats._MoveSpeed * transform.right.x, _Body.linearVelocity.y);
    }
}
