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
    CpuBaseState _currentState;
    public Dictionary<State, CpuBaseState> _State = new Dictionary<State, CpuBaseState>();

    [HideInInspector] public Stats _AttackingStats;
    public ScriptableStats _ScrStats => _Stats.scriptableStats;

    void Start()
    {
        toggleEverything(false);
        _Stats.InitializeStats();

        _State[State.Move] = new CpuMoveState(this);
        _State[State.Attack] = new CpuAttackState(this);
        _State[State.KnockBack] = new CpuKnockBackState(this);

        StartCoroutine(Startup());
    }

    IEnumerator Startup()
    {
        UpdateCurrentState(State.Move);

        yield return new WaitUntil(() => transform.childCount >= _ScrStats._Sprites.Length);
        yield return new WaitForSeconds(0.5f);
        replaceAnimation();
        toggleEverything(true);
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
        if(_Animator != null)
            _Animator.Rebind();

        _currentState = _State[state];
        _currentState.EnterState();
    }

    void replaceAnimation()
    {
        Transform _cpuRig = transform.Find("Appearance")?.Find("Rig");
        if (_cpuRig == null || _ScrStats._animator == null)
        {
            Debug.LogWarning("No Cpu Rig for animation!!");
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