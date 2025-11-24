using System.Collections;
using UnityEngine;

public class LevelTransition : MonoBehaviour
{
    public Animator TransitionAnimation;
    [SerializeField] LevelLoader levelLoader;

    public float transitionTime = 1f;

    // FInd way to avoid using update
    // Update is called once per frame
    public void StartAnimationLevel1()
    {
        StartCoroutine(LoadLevel("this week"));
    }

    public void StartAnimationLevel2()
    {
        StartCoroutine(LoadLevel("pathSelector"));
    }

    IEnumerator LoadLevel(string levelName)
    {
        TransitionAnimation.SetTrigger("Start");

        yield return new WaitForSeconds(transitionTime); //make this wait for animation to end

        levelLoader.LoadLevel(levelName);
    }

    //public void StartAnimation(int sceneID)
    //{
    //    transition.SetTrigger("Start");

    //    yield return new WaitForSeconds(transitionTime);
    //}
}
