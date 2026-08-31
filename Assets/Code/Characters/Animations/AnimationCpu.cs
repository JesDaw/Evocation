using System.Collections;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class AnimationCpu : MonoBehaviour
{
    public Animator _Animator;
    public AnimationEventsController _AnimatorController;
    public ScriptableStats _ScrStats;
    [SerializeField] Transform _rigTransform;

    public bool _flip;
    public bool _quickEdit = true;

#if UNITY_EDITOR
    bool _refreshQueued;
#endif

    void OnValidate()
    {
#if UNITY_EDITOR
        if (!_quickEdit || _refreshQueued) return;

        // OnValidate runs during Unity's serialization callback, where DestroyImmediate
        // is not permitted to take effect - it silently fails, leaving the old rig in
        // place while a new one still gets instantiated on top of it. Deferring with
        // delayCall runs the swap after serialization finishes, once destroying is safe.
        _refreshQueued = true;
        EditorApplication.delayCall += () =>
        {
            _refreshQueued = false;
            if (this == null) return; // object may have been deleted before this ran
            replaceAnimation();
        };
#endif
    }

    [ContextMenu("Update Animation")]
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
                DestroyImmediate(existing.gameObject, true);

            spriteData.Rig.transform.position = new Vector3(
                spriteData.Offset.x,
                spriteData.Offset.y,
                spriteData.Rig.transform.position.z
            );

            spriteData.Rig.transform.rotation = _flip ? Quaternion.Euler(0, 180, 0) : Quaternion.identity;

            GameObject newRig = Instantiate(spriteData.Rig, _cpuRig);
            newRig.name = rigName;

            if (rigName != "IdleRig") newRig.SetActive(false);
        }

        _Animator.runtimeAnimatorController = _ScrStats._animator;
    }

    public void SetIsRunning(bool _running) => _Animator.SetBool("IsRunning", _running);
    public void SetIsAttacking(bool _attacking) => _Animator.SetBool("IsAttacking", _attacking);
    public void SetIsKnockback(bool _knockback) => _Animator.SetBool("IsKnockback", _knockback);
}