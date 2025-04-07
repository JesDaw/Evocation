using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelLoader : MonoBehaviour
{
    public Animator transition;

    public float transitionTime = 1f;

    // FInd way to avoid using update
    // Update is called once per frame
    public void StartAnimation()
    {
        if(Input.GetMouseButtonDown(0))
        {
            StartCoroutine(LoadLevel("Week 6 Stuff"));
        }
    }

    IEnumerator LoadLevel(string levelName)
    {
        transition.SetTrigger("Start");

        yield return new WaitForSeconds(transitionTime);

        SceneManager.LoadScene(levelName);
    }
}
