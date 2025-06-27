using UnityEngine;

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
        SwapBuilding(ScrStats);
    }

    public void SetMax()
    {
        _Stats._MaxHealth = ScrStats._MaxHealth;
    }

    public void SwapBuilding(ScriptableStats _ScrStats)
    {
        _Stats._Clan = _ScrStats._Clan;
        gameObject.tag = _Stats._Clan;
        _Stats._CurrentHealth = _ScrStats._CurrentHealth;
        _Stats._AttackDamage = _ScrStats._AttackDamage;
        _Stats._AttackEndlag = _ScrStats._AttackEndlag;
        _Stats._MoveSpeed = _ScrStats._MoveSpeed;

        float randomNumber = Random.Range(-0.3f, 0.3f);
        _Stats._StopDistance = _ScrStats._StopDistance + randomNumber;
        _Stats._CpuPriority = _ScrStats._CpuPriority;

        _Stats._KnockBackHealth = _ScrStats._KnockBackHealth;
        _Stats._KnockBackVelocity = _ScrStats._KnockBackVelocity;
        _Stats._KnockBackMax = _ScrStats._KnockBackHealth;

        _Stats._StatusHealth = _ScrStats._StatusHealth;
        _Stats._StatusMax = _ScrStats._StatusHealth;

        _Renderer.sprite = _ScrStats._Sprite;
    }
    public void SwapAccordingToWho(bool IsEnemy)
    {
        if (IsEnemy == true)
        {
            SwapBuilding(EnemyBuilding);
        }
        else
        {
            SwapBuilding(AllyBuilding);
        }
    }
}
