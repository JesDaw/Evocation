using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using DG.Tweening.Core.Enums;
public class CpuStateManager : MonoBehaviour
{
    public enum State
    {
        Move,
        Attack,
        KnockBack,
    }
    public ScriptableStats _ScrStats;
    public Stats _Stats;
    public Rigidbody2D _Body;
    public Transform _Raycast;
    public Animator _Animator;
    public AnimationEventsController _AnimatorController;
    CpuBaseState _currentState;

    public Dictionary<State, CpuBaseState> _State = new Dictionary<State, CpuBaseState>();
    [SerializeField] internal UltEvents.UltEvent<Stats> OnInitStats;

    //[HideInInspector]
    public Stats _AttackingStats;

    void toggleEverything(bool enable)
    {
        var components = GetComponents<MonoBehaviour>();
        foreach (var comp in components)
        {
            if (comp != this)
            {
                comp.enabled = enable;
            }
        }

        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(enable);
        }
    }

    void Start()
    {
        toggleEverything(false);
        if(_Stats._Enemy)
            _Stats._Clan = Evocation.Clans.ClansList.Enemy;
        else
            _Stats._Clan = Evocation.Clans.ClansList.Allies;

        gameObject.tag = _Stats._Clan.ToString();
        _Stats._MaxHealth = _ScrStats._MaxHealth;
        _Stats._CurrentHealth = _ScrStats._MaxHealth;

        // info
        _Stats._MoveSpeed = _ScrStats._MoveSpeed;

        if(!_Stats._Enemy && !_Stats._CpuPriority.Contains(Evocation.Clans.ClansList.Enemy))
            _Stats._CpuPriority.Insert(0, Evocation.Clans.ClansList.Enemy);

        if(_Stats._Enemy && !_Stats._CpuPriority.Contains(Evocation.Clans.ClansList.Player))
            _Stats._CpuPriority.Insert(0, Evocation.Clans.ClansList.Player);

        if(_Stats._Enemy && !_Stats._CpuPriority.Contains(Evocation.Clans.ClansList.Allies))
            _Stats._CpuPriority.Insert(0, Evocation.Clans.ClansList.Allies);

        //knockback
        _Stats._KnockBackHealth = _ScrStats._KnockBackMax;
        _Stats._KnockBackMax = _ScrStats._KnockBackMax;

        OnInitStats.Invoke(_Stats);

        _State[State.Move] = new CpuMoveState(this);
        _State[State.Attack] = new CpuAttackState(this);

        _State[State.KnockBack] = new CpuKnockBackState(this);

        StartCoroutine(Startup());
    }
    IEnumerator Startup()
    {
        UpdateCurrentState(State.Move);
        yield return new WaitUntil(() => transform.childCount >= _ScrStats._Sprites.Length);
        replaceAnimation();
        toggleEverything(true);

        _Animator.Rebind();
        _Animator.Update(0f);

        UpdateCurrentState(State.Move);
    }

    public void UpdateCurrentState(State state)
    {
        _Animator.Rebind();

        _currentState = _State[state];
        _currentState.EnterState();
    }

    void replaceAnimation()
    {
        Transform _cpuRig = transform.Find("CpuAppearance")?.Find("CpuRig");
        if (_cpuRig == null || _ScrStats._animator == null)
        {
            Debug.LogWarning("No Cpu Rig!! (for animation)");
            return;
        }

        for (int i = 0; i < _ScrStats._Sprites.Length; ++i)
        {
            var spriteData = _ScrStats._Sprites[i];
            string rigName = null;

            switch (spriteData.Key)
            {
                case animationRigs.animationKey.Idle: rigName = "IdleRig"; break;
                case animationRigs.animationKey.Running: rigName = "RunningRig"; break;
                case animationRigs.animationKey.Knockback: rigName = "KnockbackRig"; break;
                case animationRigs.animationKey.Attack: rigName = "AttackingRig"; break;
                default: continue;
            }

            var existing = _cpuRig.Find(rigName);
            if (existing != null)
                Destroy(existing.gameObject);

            spriteData.Rig.transform.position = new Vector3(
                spriteData.Offset.x,
                spriteData.Offset.y,
                spriteData.Rig.transform.position.z
            );

            if (_ScrStats._Rotate)
            {
                spriteData.Rig.transform.rotation = Quaternion.Euler(0, 180, 0);
            }
            else
            {
                spriteData.Rig.transform.rotation = Quaternion.identity;
            }

            GameObject newRig = Instantiate(spriteData.Rig, _cpuRig);
            newRig.name = rigName;

            if (_Stats._Enemy)
                newRig.transform.Rotate(0, 180, 0);
            else
                newRig.transform.Rotate(0, -180, 0);

            if(rigName != "RunningRig") newRig.SetActive(false);
        }

        _Animator.runtimeAnimatorController = _ScrStats._animator;
    }

    void Update()
    {
        _currentState.UpdateState();
    }

}
