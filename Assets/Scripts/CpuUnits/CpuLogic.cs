using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class CpuLogic : MonoBehaviour
{
    public ScriptableStats ScrStats;
    [SerializeField] Stats _Stats;
    //this is for testing
    [Header("Req Components")]
    [SerializeField] Transform _Raycast;
    [SerializeField] SpriteRenderer _Renderer;
    [SerializeField] Rigidbody2D _Body;
    [Header("Events")]
    [SerializeField] UnityEvent OnSpawn;
    [SerializeField] CpuUtilis Utilis;
    private bool _AlreadyAttacked = false;
    private bool _Freeze = false;
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

        //knockback
        _Stats._KnockBackHealth = ScrStats._KnockBackHealth;
        _Stats._KnockBackVelocity = ScrStats._KnockBackVelocity;
        _Stats._KnockBackMax = ScrStats._KnockBackHealth;

        //status effects
        _Stats._StatusHealth = ScrStats._StatusHealth;
        _Stats._StatusMax = ScrStats._StatusHealth;
        
        _Renderer.sprite = ScrStats._Sprite;

        OnSpawn.Invoke();
    }
    void Update()
    {
        if(_Freeze) return;

        RaycastHit2D[] hits = Physics2D.RaycastAll(_Raycast.position, transform.right, _Stats._StopDistance);
        Debug.DrawRay(_Raycast.position, transform.right * _Stats._StopDistance, Color.red);
        
        //what to do when detect something
        _Stats._Speed = ScrStats._Speed;
        if (hits.Length <= 0) return;

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
        
        if (_AlreadyAttacked) return;
        StartCoroutine(AttackCooldown());

        //Utilis.SpawnMobs
        for(int I = 0; I < ScrStats.OnAttack.Length; I++)
        {
            Utilis.SelectOnAttack(I, ScrStats.ExtraStats);
        }
        //Enemy Attack
        Debug.Log("Attacked Enemy" + hits[SavedIndex].collider.gameObject.name);
        EnemyStats.Attack(_Stats._Attack);
        
        //Status Effects
        if(EnemyStats._StatusHealth <= 0)
        {
            if(ScrStats._EffectsToApply.Count == 0) return;

            foreach(StatusEffect effect in ScrStats._EffectsToApply)
            {
                if(effect is null) return;
                Debug.Log("Effect Applied");
                EnemyStats.AddStatusEffect(effect);
            }
        }
        else
        {
            EnemyStats._StatusHealth--;
        }
    }
    public void ApplyTempSpeed(Vector2 _SpeedInfo)
    {
        StartCoroutine(TempSpeed(_SpeedInfo.x));
    }
    IEnumerator TempSpeed(float _TempSpeed)
    {
        Debug.Log(_TempSpeed);

        _Freeze = true;
        _Stats._Speed = _TempSpeed;

        //x knockback scales exponentially, which is a problem...
        //but it works fine
        //and I'm too lazy to test values
        yield return new WaitForSeconds(ScrStats._Speed/6f);
        _Freeze = false;
    }

    IEnumerator AttackCooldown()
    {
        _AlreadyAttacked = true;
        yield return new WaitForSeconds(_Stats._AttackSpeed);
        _AlreadyAttacked = false;
    }

    void FixedUpdate()
    {
        _Body.linearVelocity = new Vector2(_Stats._Speed * transform.right.x, _Body.linearVelocity.y);
    }
}
