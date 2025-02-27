using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class CpuLogic : MonoBehaviour
{
    public ScriptableStats ScrStats;
    [SerializeField] Stats _Stats;
    //this is for testing
    [Header("Req Components")]
    [SerializeField] Transform _Raycast;
    [SerializeField] SpriteRenderer _Renderer;
    [SerializeField] Rigidbody2D _Body;
    private bool AlreadyAttacked = false;

    //initialize stats of NPC
    void Start()
    {
        //Set Stats class to ScriptableObject
        _Stats._Clan = ScrStats._Clan;
        gameObject.tag = _Stats._Clan;
        _Stats._Health = ScrStats._Health;
        _Stats._Attack = ScrStats._Attack;
        _Stats._AttackSpeed = ScrStats._AttackSpeed;
        _Stats._Speed = ScrStats._Speed;

        //just looks better if they slightyoffset
        float randomNumber = Random.Range(-0.3f, 0.3f);
        _Stats._StopDistance = ScrStats._StopDistance + randomNumber;
        _Stats._CpuPriority = ScrStats._CpuPriority;
        _Stats._KnockBackHealth = ScrStats._KnockBackHealth;
        _Stats._KnockBackMax = ScrStats._KnockBackHealth;
        _Stats._KnockBackVelocity = ScrStats._KnockBackVelocity;
        _Stats._KnockBackTime = ScrStats._KnockBackTime;
        
        _Renderer.sprite = ScrStats._Sprite;
    }
    void Update()
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(_Raycast.position, transform.right, _Stats._StopDistance);
        Debug.DrawRay(_Raycast.position, transform.right * _Stats._StopDistance, Color.red);
        
        //what to do when detect something
        if (hits.Length > 0)
        {
            int SavedIndex = -1;
            for (int I = 0; I < _Stats._CpuPriority.Count; I++)
            {
                for (int II = 0; II < hits.Length; II++)
                {
                    if (hits[II].collider.CompareTag(_Stats._CpuPriority[I]))
                    {
                        SavedIndex = II;
                        break;
                    }
                }
                if (SavedIndex != -1)
                {
                    break;
                }
            }

            if (SavedIndex == -1) return;

            _Stats._Speed = 0;

            Stats EnemyStats = hits[SavedIndex].collider.gameObject.GetComponent<Stats>();
            if (EnemyStats == null) return;
            
            if (AlreadyAttacked) return;
            StartCoroutine(AttackCooldown());

            Debug.Log("Attacked Enemy" + hits[SavedIndex].collider.gameObject.name);

            EnemyStats.Attack(_Stats._Attack);
        }
        else
        {
            _Stats._Speed = ScrStats._Speed;
        }
    }
    public void ApplyTempSpeed(Vector2 _SpeedInfo)
    {
        if(_Freeze) return;
        StartCoroutine(TempSpeed(_SpeedInfo.x));
    }
    IEnumerator TempSpeed(float _TempSpeed)
    {
        Debug.Log(_TempSpeed);

        _Stats._Speed = _TempSpeed;
        _Freeze = true;

        //if you know phycis you can prob use some
        //magic to calculate this
        //but i'm studpi
        yield return new WaitForSeconds(_Stats._KnockBackTime);
        _Freeze = false;
    }

    IEnumerator AttackCooldown()
    {
        AlreadyAttacked = true;
        yield return new WaitForSeconds(_Stats._AttackSpeed);
        AlreadyAttacked = false;
    }

    //normilize walk speed
    void FixedUpdate()
    {
        _Body.linearVelocity = new Vector2(_Stats._Speed * transform.right.x, _Body.linearVelocity.y);
    }
}
