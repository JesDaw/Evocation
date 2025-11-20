using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Evocation.Clans;

public class BuildingStatsManager : MonoBehaviour
{
    //this scrip is cpuLogic but for buildings
    public SpriteRenderer _Renderer;
    public ScriptableStats ScrStats;
    public Stats _Stats;
    [SerializeField] GameObject Building;
    [Header("Buildings")]
    [SerializeField] ScriptableStats AllyBuilding;
    [SerializeField] ScriptableStats EnemyBuilding;
    void Start()
    {
        gameObject.tag = "Blank";
        if (_Renderer == null)
        {
            _Renderer = gameObject.GetComponent<SpriteRenderer>();
            Debug.Log($"auto linked sprite renderor onto {gameObject.name} because it wasnt set in inspecter");
        }
        SwapBuilding(ScrStats);
    }

    public void SetMax()
    {
        _Stats._MaxHealth = ScrStats._MaxHealth;
    }

    public void SwapBuilding(ScriptableStats _ScrStats)
    {
        if(_Stats._Enemy)
            _Stats._Clan = ClansList.Enemy;
        else
            _Stats._Clan = ClansList.Allies;

        gameObject.tag = _Stats._Clan.ToString();
        _Stats._CurrentHealth = _ScrStats._MaxHealth;
        _Stats._AttackDamage = _ScrStats._AttackDamage;
        _Stats._AttackEndlag = _ScrStats._AttackEndlag;
        _Stats._MoveSpeed = _ScrStats._MoveSpeed;

        float randomNumber = Random.Range(-0.3f, 0.3f);
        _Stats._StopDistance = _ScrStats._StopDistance + randomNumber;

        _Stats._KnockBackHealth = _ScrStats._KnockBackMax;
        _Stats._KnockBackVelocity = _ScrStats._KnockBackVelocity;
        _Stats._KnockBackMax = _ScrStats._KnockBackMax;

        _Stats._StatusHealth = _ScrStats._StatusHealth;
        _Stats._StatusMax = _ScrStats._StatusHealth;
        _Stats._StatusEffects = new List<StatusEffect>();
        _Stats._StatusTicksMax = new List<Vector2>();
        _Stats._StatusTicks = new List<Vector2>();
    }
    public void SwapAccordingToWho(bool IsEnemy)
    {
        if (IsEnemy)
        {
            SwapBuilding(EnemyBuilding);
            gameObject.tag = ClansList.Enemy.ToString();
        }
        else
        {
            SwapBuilding(AllyBuilding);
            gameObject.tag = ClansList.Allies.ToString();
        }

        //delay is needed because status effect runs on update()
        StartCoroutine(ResetDestroyedAfterDelay());
    }
    public void SwapToPlayer()
    {
        Debug.Log("player claimed building");
        SwapBuilding(AllyBuilding);
        gameObject.tag = ClansList.Allies.ToString();
    }

    private IEnumerator ResetDestroyedAfterDelay()
    {
        yield return new WaitForSeconds(0.5f); // wait for status effects and any prelimary attacks
        _Stats.SetDestroyed(false);
    }
}
