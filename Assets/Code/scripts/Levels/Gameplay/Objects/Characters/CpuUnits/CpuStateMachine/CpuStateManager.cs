using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class CpuStateManager : MonoBehaviour
{
    public enum State
    {
        Move,
        Attack,
        KnockBack,
    }

    [Header("References")]
    public Stats _Stats;
    public Rigidbody2D _Body;
    public Animator _Animator;
    public AnimationEventsController _AnimatorController;

    [Header("State Management")]
    public State CurrentState {get; private set;}
    CpuBaseState _currentState;
    public Dictionary<State, CpuBaseState> _State = new Dictionary<State, CpuBaseState>();

    [HideInInspector] public Stats _AttackingStats;
    public ScriptableStats _ScrStats => _Stats.scriptableStats;
    public Action<State> OnCPUStateChange = delegate { };

    void Start()
    {
        toggleEverything(false);
       // _Stats.InitializeStats();

        _State[State.Move] = new CpuMoveState(this);
        _State[State.Attack] = new CpuAttackState(this);
        _State[State.KnockBack] = new CpuKnockBackState(this);

        StartCoroutine(Startup());
    }

    bool MatchHierarchy(Transform a, Transform b)
    {
        if (a.childCount != b.childCount)
            return false;

        for (int i = 0; i < a.childCount; i++)
        {
            if (!MatchHierarchy(a.GetChild(i), b.GetChild(i)))
                return false;
        }

        return true;
    }

    IEnumerator Startup()
    {
        replaceAnimation();
        toggleEverything(true);
        yield return new WaitForSeconds(0.1f);
        if(_Animator != null)
        {
            _Animator.Rebind();
            _Animator.Update(0f);
        }
        UpdateCurrentState(State.Move);
    }

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

    public void UpdateCurrentState(State state)
    {
        _currentState = _State[state];
		CurrentState = state;
        _currentState.EnterState();
        OnCPUStateChange?.Invoke(state);
    }

    void replaceAnimation()
    {
        Transform _cpuRig = transform.Find("Appearance")?.Find("Rig");
        if (_cpuRig == null || _ScrStats._animator == null)
        {
            Debug.LogWarning($"No Cpu Rig for animation on {gameObject.name}: _cpuRig == null {_cpuRig == null}, _ScrStats._animator == null {_ScrStats._animator == null}");
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
        _currentState?.UpdateState();
    }
}
