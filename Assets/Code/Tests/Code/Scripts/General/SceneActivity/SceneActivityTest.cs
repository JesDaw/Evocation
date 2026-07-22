using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

using UnityEngine.SceneManagement;

public class SceneActivityTest
{ /*
    GameObject myTestObj;
    SceneActivity mySA;


    [UnitySetUp]
    public IEnumerator LoadSceneObjects()
    {
        SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
        yield return null;

        Scene scene = SceneManager.GetSceneByName("MainMenu");
        GameObject origObj = GameObject.Find("MainMenu");

        myTestObj = GameObject.Instantiate(origObj);

        mySA = myTestObj.GetComponent<SceneActivity>();

        yield return null;
    }

    [UnityTest]
    public IEnumerator StartActivityEnablesObjectByDefault()
    {
        myTestObj.SetActive(false);

        mySA.StartActivity();

        Assert.IsTrue(myTestObj.activeSelf);

        yield return null;
    }

    [UnityTest]
    public IEnumerator StartActivityRaisesStartEvent()
    {
        bool wasRaised = false;

        mySA.OnActivityStart.AddListener(() => wasRaised = true);
        mySA.StartActivity();

        Assert.IsTrue(wasRaised);
        yield return null;
    }

    [UnityTest]
    public IEnumerator StartActivityWithDisabledDefaultDoesNothing()
    {
        myTestObj.SetActive(false);

        mySA.disableDefaultBehavior = true;
        mySA.StartActivity();

        Assert.IsFalse(myTestObj.activeSelf);

        yield return null;
    }

    [UnityTest]
    public IEnumerator StopActivityDisablesObjectByDefault()
    {
        myTestObj.SetActive(true);

        mySA.StopActivity();

        Assert.IsFalse(myTestObj.activeSelf);

        yield return null;
    }

    [UnityTest]
    public IEnumerator StopActivityRaisesStopEvent()
    {
        bool wasRaised = false;

        mySA.OnActivityStop.AddListener(() => wasRaised = true);
        mySA.StopActivity();

        Assert.IsTrue(wasRaised);
        yield return null;
    }

    [UnityTest]
    public IEnumerator StopActivityWithDisabledDefaultDoesNothing()
    {
        myTestObj.SetActive(true);

        mySA.disableDefaultBehavior = true;
        mySA.StopActivity();

        Assert.IsTrue(myTestObj.activeSelf);

        yield return null;
    } */
}
