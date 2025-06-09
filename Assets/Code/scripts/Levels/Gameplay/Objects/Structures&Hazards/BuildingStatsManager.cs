using UnityEngine;

public class BuildingStatsManager : MonoBehaviour
{
    //this scrip is cpuLogic but for buildings
    public SpriteRenderer _Renderer;
    public ScriptableStats ScrStats;
    public Stats _Stats;
    [SerializeField] GameObject Building;
    [SerializeField] string AllyBuilding;
    void Start()
    {
        _Stats._Clan = ScrStats._Clan;
        gameObject.tag = _Stats._Clan;
        _Stats._Health = ScrStats._Health;
        _Stats._AttackDamage = ScrStats._AttackDamage;
        _Stats._AttackEndlag = ScrStats._AttackEndlag;
        _Stats._MoveSpeed = ScrStats._MoveSpeed;

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
    }

    public void SetMax()
    {
        _Stats._Health = ScrStats._Health;
    }

    public void ChangeTeam(string _Team)
    {
        AllyBuilding = _Team;

        if (AllyBuilding == "LavaBros")
        {
            _Stats._Clan = "LavaBros";
            Building.tag = "LavaBros";
        }
        else
        {
            _Stats._Clan = "TreeGang";
            Building.tag = "TreeGang";
        }
    }
    public void ChangeColor()
    {
        if (AllyBuilding == "LavaBros") _Renderer.color = Color.red;
        if (AllyBuilding == "TreeGang") _Renderer.color = Color.green;
    }
}
