using System.Collections.Generic;
using System.Collections;
using UnityEngine;

/// <summary>
/// Manages CPU state machine - focused on STATE MANAGEMENT only
/// All stats initialization now handled by Stats component itself
/// </summary>
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

    // Quick accessor for ScriptableStats
    public ScriptableStats _ScrStats => _Stats.scriptableStats;

    void Start()
    {
        // Disable everything during setup
        toggleEverything(false);
        
        // Let Stats handle its own initialization (clan, priorities, values)
        _Stats.InitializeStats();

        // Initialize state machine
        _State[State.Move] = new CpuMoveState(this);
        _State[State.Attack] = new CpuAttackState(this);
        _State[State.KnockBack] = new CpuKnockBackState(this);

        // Start delayed startup for animation setup
        StartCoroutine(Startup());
    }

    /// <summary>
    /// Delayed startup - waits for animation rigs to be ready before enabling everything
    /// </summary>
    IEnumerator Startup()
    {
        UpdateCurrentState(State.Move);
        
        // Wait until all sprite rigs are instantiated as children
        yield return new WaitUntil(() => transform.childCount >= _ScrStats._Sprites.Length);
        
        // Extra safety delay for animations to settle
        yield return new WaitForSeconds(0.5f);
        
        // Set up visual appearance from ScriptableStats
        replaceAnimation();
        
        // Now enable all components and children
        toggleEverything(true);

        // Reset animator
        if(_Animator != null)
        {
            _Animator.Rebind();
            _Animator.Update(0f);
        }

        // Start in Move state
        UpdateCurrentState(State.Move);
    }

    /// <summary>
    /// Disable/enable all components and children except CpuStateManager
    /// Prevents scripts from running before CPU is fully initialized
    /// </summary>
    void toggleEverything(bool enable)
    {
        // Toggle all MonoBehaviours on this GameObject (except this one)
        var components = GetComponents<MonoBehaviour>();
        foreach (var comp in components)
        {
            if (comp != this)
            {
                comp.enabled = enable;
            }
        }

        // Toggle all child GameObjects
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

    /// <summary>
    /// Replaces animation rigs based on ScriptableStats sprite data
    /// This is what makes each CPU look different!
    /// </summary>
    void replaceAnimation()
    {
        Transform _cpuRig = transform.Find("Appearance")?.Find("Rig");
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
        _currentState?.UpdateState();
    }
}