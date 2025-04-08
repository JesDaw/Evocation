using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelLoader : MonoBehaviour
{
    public Animator transition;

    public float transitionTime = 1f;

    // FInd way to avoid using update
    // Update is called once per frame
    public void StartAnimationLevel1()
    {
        StartCoroutine(LoadLevel("Week 7&8 Stuff"));
    }
        public void StartAnimationLevel2()
    {
        StartCoroutine(LoadLevel("AI Movement"));
    }

    IEnumerator LoadLevel(string levelName)
    {
        transition.SetTrigger("Start");

        yield return new WaitForSeconds(transitionTime);

        SceneManager.LoadScene(levelName);
    }
}
