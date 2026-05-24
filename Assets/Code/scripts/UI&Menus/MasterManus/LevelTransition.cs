using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class LevelTransition : MonoBehaviour
{
    [Header("Animation stuff")]
    [SerializeField] Animator TransitionAnimationClip;
    [SerializeField] float transitionTime = 1f;
    [SerializeField] string timelineCutsceneName;

    [Header("Loading screen stuff")]
    [SerializeField] GameObject loadingScreen;
    [SerializeField] Slider slider;
    [SerializeField] TextMeshProUGUI progressText;
    [Header("Next scene")]
    [SerializeField] string NextSceneName;
    [SerializeField] bool DebugLogs = false;

    public void StartTransition()
    {
        StartCoroutine(LoadScene(NextSceneName));
    }

    IEnumerator LoadScene(string nextSceneName)
    {
        if(TransitionAnimationClip != null)
        {
            TransitionAnimationClip.SetTrigger("Start");
            yield return new WaitForSeconds(transitionTime); 
        }
        else if (TimelineManager.Instance != null && !string.IsNullOrEmpty(timelineCutsceneName))
        {
            TimelineManager.Instance.PlayCutscene(timelineCutsceneName);
            yield return new WaitForSeconds(TimelineManager.Instance.GetCurrentCutsceneDuration());
        }
        if (DebugLogs) Debug.Log($"Loading scene: " + nextSceneName);
        StartCoroutine(LoadAsynchronously(nextSceneName));
    }
    

    IEnumerator LoadAsynchronously(string sceneName)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);

        loadingScreen.SetActive(true);
        if (DebugLogs) Debug.Log($"Loading screen active");
        
        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f);

            slider.value = progress;
            progressText.text = (progress * 100f).ToString("F0") + "%"; 

            yield return null;
        }
    }
}
