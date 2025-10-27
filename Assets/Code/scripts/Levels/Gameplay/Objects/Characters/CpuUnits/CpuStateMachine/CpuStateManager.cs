using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine;
using UnityEngine.Analytics;
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
    public AnimatorOverrideController _OverrideController;
    CpuBaseState _currentState;

    public Dictionary<State, CpuBaseState> _State = new Dictionary<State, CpuBaseState>();
    [SerializeField] internal UltEvents.UltEvent<Stats> OnInitStats;

    //[HideInInspector]
    public Stats _AttackingStats;

    void Start()
    {
        _Stats._Clan = _ScrStats._Clan;
        gameObject.tag = _Stats._Clan;
        _Stats._MaxHealth = _ScrStats._MaxHealth;
        _Stats._CurrentHealth = _ScrStats._CurrentHealth;
        _Stats._AttackDamage = _ScrStats._AttackDamage;
        _Stats._AttackEndlag = _ScrStats._AttackEndlag;
        _Stats._MoveSpeed = _ScrStats._MoveSpeed;

        //just looks better if they slightyoffset
        float randomNumber = Random.Range(-0.3f, 0.3f);
        _Stats._StopDistance = _ScrStats._StopDistance + randomNumber;
        _Stats._CpuPriority = _ScrStats._CpuPriority;
        //if(_Stats._Enemy) _Stats._CpuPriority.Insert(0, "Player");

        //knockback
        _Stats._KnockBackHealth = _ScrStats._KnockBackHealth;
        _Stats._KnockBackVelocity = _ScrStats._KnockBackVelocity;
        _Stats._KnockBackMax = _ScrStats._KnockBackHealth;

        //status effects
        _Stats._StatusHealth = _ScrStats._StatusHealth;
        _Stats._StatusMax = _ScrStats._StatusHealth;

        OnInitStats.Invoke(_Stats);


        _State[State.Move] = new CpuMoveState(this);
        _State[State.Attack] = new CpuAttackState(this);

        _State[State.KnockBack] = new CpuKnockBackState(this);

        if (_ScrStats._Sprites.Length > 0) replaceAnimation(); 

        UpdateCurrentState(State.Move);
    }
    public void UpdateCurrentState(State state)
    {
        _currentState = _State[state];
        _currentState.EnterState();
    }

    void replaceAnimation()
    {
        AnimatorOverrideController runtimeOverride = new AnimatorOverrideController(_OverrideController);
        Transform _cpuRig = transform.Find("CpuAppearance")?.transform.Find("CpuRig");
        if (_cpuRig == null) { Debug.LogWarning("No Cpu Rig!! (for animation)"); return; }
        for (int I = 0; I < _ScrStats._Sprites.Length; ++I)
        {

            switch (_ScrStats._Sprites[I].Key)
            {
                //looks really intemidating but it's pretty simpele
                case (animationRigs.animationKey.Idle):
                    {
                        GameObject currentRig = _cpuRig?.Find("IdleRig").gameObject;
                        if (currentRig != null) Destroy(currentRig);

                        Transform TempTransform = _cpuRig;
                        TempTransform.position = new Vector3
                                                    (
                                                        transform.position.x + _ScrStats._Sprites[I].Offset.x,
                                                        transform.position.y + _ScrStats._Sprites[I].Offset.y,
                                                        transform.position.z
                                                    );

                        GameObject newRig = Instantiate(_ScrStats._Sprites[I].Rig, TempTransform);
                        Debug.Log(newRig.name);
                        if (_Stats._Enemy) newRig.transform.Rotate(0, 180, 0);
                        runtimeOverride["HoodesIdle"] = _ScrStats._Sprites[I].Animation;
                    }
                    break;
                case (animationRigs.animationKey.Running):
                    {
                        GameObject currentRig = _cpuRig?.Find("RunningRig").gameObject;
                        if (currentRig != null) Destroy(currentRig);

                        Transform TempTransform = _cpuRig;
                        TempTransform.position = new Vector3
                                                    (
                                                        transform.position.x + _ScrStats._Sprites[I].Offset.x,
                                                        transform.position.y + _ScrStats._Sprites[I].Offset.y,
                                                        transform.position.z
                                                    );

                        GameObject newRig = Instantiate(_ScrStats._Sprites[I].Rig, TempTransform);
                        Debug.Log(newRig.name);
                        if (_Stats._Enemy) newRig.transform.Rotate(0, 180, 0);
                        runtimeOverride["HoodesRunning"] = _ScrStats._Sprites[I].Animation;
                    }
                    break;
                case (animationRigs.animationKey.Knockback):
                    {
                        GameObject currentRig = _cpuRig?.Find("KnockbackRig").gameObject;
                        if (currentRig != null) Destroy(currentRig);

                        Transform TempTransform = _cpuRig;
                        TempTransform.position = new Vector3
                                                    (
                                                        transform.position.x + _ScrStats._Sprites[I].Offset.x,
                                                        transform.position.y + _ScrStats._Sprites[I].Offset.y,
                                                        transform.position.z
                                                    );

                        GameObject newRig = Instantiate(_ScrStats._Sprites[I].Rig, TempTransform);
                        Debug.Log(newRig.name);
                        if (_Stats._Enemy) newRig.transform.Rotate(0, 180, 0);
                        runtimeOverride["HoodeesKnockback"] = _ScrStats._Sprites[I].Animation;
                    }
                    break;
                case (animationRigs.animationKey.Attack):
                    {
                        GameObject currentRig = _cpuRig?.Find("AttackingRig").gameObject;
                        if (currentRig != null) Destroy(currentRig);
                        Transform TempTransform = _cpuRig;
                        TempTransform.position = new Vector3
                                                    (
                                                        transform.position.x + _ScrStats._Sprites[I].Offset.x,
                                                        transform.position.y + _ScrStats._Sprites[I].Offset.y,
                                                        transform.position.z
                                                    );

                        GameObject newRig = Instantiate(_ScrStats._Sprites[I].Rig, TempTransform);
                        Debug.Log(newRig.name);
                        if (_Stats._Enemy) newRig.transform.Rotate(0, 180, 0);
                        runtimeOverride["HoodesAttacking"] = _ScrStats._Sprites[I].Animation;

                    }
                    break;
                default:
                    break;
            }

        }

        _Animator.runtimeAnimatorController = runtimeOverride;
        return;
    }

    void Update()
    {
        _currentState.UpdateState();
    }
}
