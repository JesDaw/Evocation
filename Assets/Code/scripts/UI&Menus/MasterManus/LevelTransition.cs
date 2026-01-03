using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro; 

public class LevelTransition : MonoBehaviour
{
    public Animator TransitionAnimation;
    public GameObject loadingScreen;
    public Slider slider;
    public TextMeshProUGUI progressText;
    public float transitionTime = 1f;
    [SerializeField] string NextSceneName;

    public void StartTransition()
    {
        StartCoroutine(LoadScene(NextSceneName));
    }

    IEnumerator LoadScene(string nextSceneName)
    {
        TransitionAnimation.SetTrigger("Start");

        yield return new WaitForSeconds(transitionTime); 

        StartCoroutine(LoadAsynchronously(nextSceneName));
    }
    

    IEnumerator LoadAsynchronously(string sceneName)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);

        loadingScreen.SetActive(true);
        
        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f);

            slider.value = progress;
            progressText.text = (progress * 100f).ToString("F0") + "%"; 

            yield return null;
        }
    }

    //public void StartAnimation(int sceneID)
    //{
    //    transition.SetTrigger("Start");

    //    yield return new WaitForSeconds(transitionTime);
    //}
}
