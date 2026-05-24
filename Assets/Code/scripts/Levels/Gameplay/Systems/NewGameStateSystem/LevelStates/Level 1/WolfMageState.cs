using UnityEngine;
using System;

[Serializable]
public class WolfMageState : LevelState
{
    [SerializeField] GameObject RippleEffectLocation;
    [SerializeField] Renderer targetRenderer;
    [SerializeField] BoxCollider2D boxCollider;
    [SerializeField] ScriptableStats BossStats; 
    [SerializeField] int FreeMoney;
    
    [Header("Brightness Tween")]
    [SerializeField] float brightnessStartValue = 0f;
    [SerializeField] float brightnessEndValue = -0.5f;
    
    [Header("Hue Shift Tween")]
    [SerializeField] float hueShiftStartValue = 0f;
    [SerializeField] float hueShiftEndValue = 0.2f;
    
    [Header("Saturation Tween")]
    [SerializeField] float saturationStartValue = 1f;
    [SerializeField] float saturationEndValue = 1.5f;
    [SerializeField] float tweenDuration = 5.0f;
    

    protected override void OnEnterState()
    {
        //Debug.Log("Entering boss state");
        VisualEffectsManager.Instance.SpawnShockwave(RippleEffectLocation.transform.position); 
        
        if (targetRenderer != null)
        {
            VisualEffectsManager.Instance.TweenShaderFloat(targetRenderer, "_BrightnessAmount", brightnessEndValue, tweenDuration, brightnessStartValue);
            VisualEffectsManager.Instance.TweenShaderFloat(targetRenderer, "_HueShiftAmount", hueShiftEndValue, tweenDuration, hueShiftStartValue);
            VisualEffectsManager.Instance.TweenShaderFloat(targetRenderer, "_SaturationAmount", saturationEndValue, tweenDuration, saturationStartValue);
        }


        if (boxCollider != null)
        {
            Collider2D[] hitColliders = Physics2D.OverlapBoxAll(RippleEffectLocation.transform.position, boxCollider.size, 0f);
            
            foreach (Collider2D hit in hitColliders)
            {
                PlayerStateMachine playerSM = hit.GetComponentInParent<PlayerStateMachine>();
                if (playerSM != null && hit.gameObject.CompareTag("Player"))
                {
                    playerSM.UpdateCurrentStateToKnockback();
                    continue;
                }
                
                CpuStateManager cpuSM = hit.GetComponentInParent<CpuStateManager>();
                if (cpuSM != null && hit.gameObject.CompareTag("Allies"))
                {
                    cpuSM.UpdateCurrentState(CpuStateManager.State.KnockBack);
                }
            }
        }

        AISpawnerController.Instance.SetMoodByName("Phase 2");
        //Debug.Log("Spawning boss");
        SpawnObjects.EnemyInstance.SpawnFromAISpawner(BossStats, true);
        //AIMoneyManager.Instance.GiveMoney(FreeMoney);
    }
}
