using System.Collections;
using UnityEngine;

public class AnimationCpu : MonoBehaviour
{
	public Animator _Animator;
	public AnimationEventsController _AnimatorController;
	public ScriptableStats _ScrStats;
    [SerializeField] Transform _rigTransform;
    
	public bool _flip;
    public bool _quickEdit = false;

    void OnValidate()
    {
        if(_quickEdit)
            replaceAnimation();
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

			if (_flip)
			{
				spriteData.Rig.transform.rotation = Quaternion.Euler(0, 180, 0);
			}
			else
			{
				spriteData.Rig.transform.rotation = Quaternion.identity;
			}

			GameObject newRig = Instantiate(spriteData.Rig, _cpuRig);
			newRig.name = rigName;

			if(rigName != "IdleRig") newRig.SetActive(false);
		}

		_Animator.runtimeAnimatorController = _ScrStats._animator;
	}

    public void SetIsRunning(bool _running)
    {
        _Animator.SetBool("IsRunning", _running);
    }

    public void SetIsAttacking(bool _attacking)
    {
        _Animator.SetBool("IsAttacking", _attacking);
    }

    public void SetIsKnockback(bool _knockback)
    {
        _Animator.SetBool("IsKnockback", _knockback);
    }
}