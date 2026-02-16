using System.Collections;
using UnityEngine;

public class AnimationCpu : MonoBehaviour
{
	public Animator _Animator;
	public AnimationEventsController _AnimatorController;
	public ScriptableStats _ScrStats;

	public bool _flip;

    void Start()
    {
		replaceAnimation();        
		//StartCoroutine(Startup());
	}

    IEnumerator Startup()
    {
		yield return new WaitUntil(() => transform.childCount >= _ScrStats._Sprites.Length);
		replaceAnimation();        
		if(_Animator != null)
		{
			_Animator.Rebind();
			_Animator.Update(0f);
		}
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

			if (_flip)
				newRig.transform.Rotate(0, 180, 0);
			else
				newRig.transform.Rotate(0, -180, 0);

			if(rigName != "RunningRig") newRig.SetActive(false);
		}

		_Animator.runtimeAnimatorController = _ScrStats._animator;
	}
}
