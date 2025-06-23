using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

public class SceneActivityManagerTest
{
    GameObject myTestObj;
    SceneActivityManager myMgr;

    GameObject myFakeSA;
    const string FAKE_SA_NAME = "SA_MyFakeSAForUnitTesting";

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
        yield return null;

        Scene scene = SceneManager.GetSceneByName("MainMenu");
        GameObject origObj = GameObject.Find("SceneActivityManager");

        GameObject origSA = origObj.GetComponent<SceneActivityManager>().InitialSA;
        myFakeSA = GameObject.Instantiate(origSA);
        myFakeSA.name = FAKE_SA_NAME;
        myFakeSA.SetActive(false);

        myTestObj = GameObject.Instantiate(origObj);

        myMgr = myTestObj.GetComponent<SceneActivityManager>();

        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        UnityEngine.Object.Destroy(myTestObj);
        UnityEngine.Object.Destroy(myFakeSA);
        yield return null;
    }

    [UnityTest]
    public IEnumerator StartCachesAllSAObjects()
    {
        // If it found my new one then I'm pretty confident it
        // found all the others
        Assert.IsTrue(myMgr.objNamed.ContainsKey(FAKE_SA_NAME));
        Assert.IsTrue(myMgr.objNamed.Count > 1);

        yield return null;
    }

    [UnityTest]
    public IEnumerator StartDetectsInitialSA()
    {
        myFakeSA.gameObject.SetActive(true);
        myMgr.InitialSA.SetActive(false);

        myMgr.Start();

        Assert.AreEqual(myMgr.InitialSA.name, FAKE_SA_NAME);

        yield return null;
    }

    [UnityTest]
    public IEnumerator StartThrowsOnMultipleInitialSAs()
    {
        myFakeSA.gameObject.SetActive(true);

        Assert.Throws<SceneActivityManager.SAException>(myMgr.Start);

        yield return null;
    }


    [UnityTest]
    public IEnumerator StartThrowsForSANamingConflict()
    {
        myFakeSA.name = myMgr.InitialSA.name;
        yield return null;

        Assert.Throws<SceneActivityManager.SAException>(myMgr.Start);
        yield return null;
    }


    [UnityTest]
    public IEnumerator ActivatePreviousSASucceeds()
    {
        var saObj = myMgr.GetCurrentActivity();

        myMgr.Activate(FAKE_SA_NAME);

        myMgr.ActivatePreviousSA();
        Assert.AreEqual(saObj, myMgr.GetCurrentActivity());

        yield return null;
    }

    [UnityTest]
    public IEnumerator ActivatePreviousAtInitialSAHasNoEffect()
    {
        var saObj = myMgr.GetCurrentActivity();

        myMgr.ActivatePreviousSA();
        yield return null;

        Assert.AreEqual(saObj, myMgr.GetCurrentActivity());

        yield return null;
    }

    [UnityTest]
    public IEnumerator ActivateByNameSucceeds()
    {
        myMgr.Activate(FAKE_SA_NAME);
        yield return null;

        string currName = myMgr.GetCurrentActivity().name;
        Assert.AreEqual(currName, FAKE_SA_NAME);

        yield return null;
    }

    [UnityTest]
    public IEnumerator InInitialSASucceeds()
    {
        Assert.IsTrue(myMgr.InInitialSA());

        yield return null;
    }

    [UnityTest]
    public IEnumerator NotInInitialSASucceeds()
    {
        myMgr.Activate(FAKE_SA_NAME);

        yield return null;
        Assert.IsFalse(myMgr.InInitialSA());

        yield return null;
    }

    [UnityTest]
    public IEnumerator ActivationInvokesChangeEvent()
    {
        bool wasCalled = false;

        myMgr.ActivityChanged.AddListener(() => wasCalled = true);
        myMgr.Activate(FAKE_SA_NAME);

        Assert.IsTrue(wasCalled);
        yield return null;
    }

    [UnityTest]
    public IEnumerator ActivationThrowsWhenSAIsNotFound()
    {
        Assert.Throws<SceneActivityManager.SAException>(() => myMgr.Activate("AnUnknownSA"));

        yield return null;
    }
}
