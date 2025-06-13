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
    const string FAKE_SA_ID = "MyFakeSAForUnitTesting";

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
        yield return null;

        Scene scene = SceneManager.GetSceneByName("MainMenu");
        GameObject origObj = GameObject.Find("SceneActivityManager");

        GameObject origSA = GameObject.Find("SA_0_MainMenu");
        myFakeSA = GameObject.Instantiate(origSA);
        myFakeSA.name = FAKE_SA_NAME;

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


    [Test]
    public void ParseNameWithOneIdentifier()
    {
        var name = SceneActivityManager.ParseSAObjectName("SA_Settings");

        Assert.IsNull(name.index);
        Assert.AreEqual(name.strName, "Settings");
    }

    [Test]
    public void ParseNameWithTwoIdentifiers()
    {
        var name = SceneActivityManager.ParseSAObjectName("SA_0_Settings");

        Assert.AreEqual(0, name.index);
        Assert.AreEqual(name.strName, "Settings");
    }

    [Test]
    public void ParseNonConformingName()
    {
        var name = SceneActivityManager.ParseSAObjectName("SASettings");

        Assert.IsNull(name.index);
        Assert.IsNull(name.strName);
    }

    [Test]
    public void ParseNullName()
    {
        var name = SceneActivityManager.ParseSAObjectName("SASettings");

        Assert.IsNull(name.index);
        Assert.IsNull(name.strName);
    }

    [Test]
    public void ParseNameWithMultipleUnderscores()
    {
        var name = SceneActivityManager.ParseSAObjectName("SA_Settings_With_Extra_Parts");

        Assert.IsNull(name.index);
        Assert.AreEqual(name.strName, "Settings_With_Extra_Parts");
    }

    [Test]
    public void CheckNameWithOneIdentifier()
    {
        var result = SceneActivityManager.IsSAObjectName("SA_Settings");

        Assert.IsTrue(result);
    }

    [Test]
    public void CheckNameWithTwoIdentifiers()
    {
        var result = SceneActivityManager.IsSAObjectName("SA_0_Settings");

        Assert.IsTrue(result);
    }


    [Test]
    public void CheckNonConformingName()
    {
        var result = SceneActivityManager.IsSAObjectName("SASettings");

        Assert.IsFalse(result);
    }

    [Test]
    public void CheckNullName()
    {
        var result = SceneActivityManager.IsSAObjectName(null);

        Assert.IsFalse(result);
    }

    [UnityTest]
    public IEnumerator StartCachesAllSAObjects()
    {
        // If it found my new one then I'm pretty confident it
        // found all the others
        Assert.IsTrue(myMgr.objNamed.ContainsKey(FAKE_SA_ID));
        Assert.IsTrue(myMgr.objNamed.Count > 1);

        yield return null;
    }

    [UnityTest]
    public IEnumerator StartActivatesInitialSAAndDisablesAllOthers()
    {
        GameObject sa0 = myMgr.objNamed["0"];
        GameObject sa1 = myMgr.objNamed["3"];
        myMgr.ActivateSA3();
        yield return null;

        Assert.IsTrue(sa1.activeSelf);
        Assert.IsFalse(sa0.activeSelf);
        myMgr.Start();
        yield return null;

        Assert.IsTrue(sa0.activeSelf);
        Assert.IsFalse(sa1.activeSelf);

        yield return null;
    }

    [UnityTest]
    public IEnumerator StartRaisesWhenSAHasBadName()
    {
        myFakeSA.name = "ASABadName";
        yield return null;

        Assert.Throws<SceneActivityManager.BadNameException>(myMgr.Start);
        yield return null;
    }

    [UnityTest]
    public IEnumerator StartRaisesWhenSAHasWrongBehaviour()
    {
        var sa = myFakeSA.GetComponent<SceneActivity>();
        UnityEngine.Object.Destroy(sa);
        yield return null;

        Assert.Throws<SceneActivityManager.BadNameException>(myMgr.Start);
        yield return null;
    }

    [UnityTest]
    public IEnumerator StartRaisesForSANamingConflict()
    {
        myFakeSA.name = "SA_0_MyFakeSAForUnitTesting2";
        yield return null;

        Assert.Throws<SceneActivityManager.BadNameException>(myMgr.Start);
        yield return null;
    }

    [UnityTest]
    public IEnumerator StartCachesBothSAIdentifiers()
    {
        myFakeSA.name = "SA_99_MyFakeSAForUnitTesting2";
        yield return null;

        myMgr.Start();
        yield return null;

        Assert.IsTrue(myMgr.objNamed.ContainsKey("99"));
        Assert.IsTrue(myMgr.objNamed.ContainsKey("MyFakeSAForUnitTesting2"));
        yield return null;
    }

    [UnityTest]
    public IEnumerator ActivateByIndexSucceeds()
    {
        myMgr.ActivateSA3();
        yield return null;

        string currName = myMgr.GetCurrentActivity().name;
        Assert.IsTrue(currName.Contains("_3_"));

        yield return null;
    }

    [UnityTest]
    public IEnumerator ActivatePreviousSASucceeds()
    {
        var saObj = myMgr.GetCurrentActivity();

        myMgr.ActivateByName(FAKE_SA_ID);
        yield return null;

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
        myMgr.ActivateByName("Credits");
        yield return null;

        string currName = myMgr.GetCurrentActivity().name;
        Assert.IsTrue(currName.Contains("Credits"));

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
        myMgr.ActivateByName(FAKE_SA_ID);

        yield return null;
        Assert.IsFalse(myMgr.InInitialSA());

        yield return null;
    }

    [UnityTest]
    public IEnumerator ActivationRaisesChangeEvent()
    {
        bool wasCalled = false;

        myMgr.ActivityChanged.AddListener(() => wasCalled = true);
        myMgr.ActivateByName(FAKE_SA_ID);
        yield return null;

        Assert.IsTrue(wasCalled);
        yield return null;
    }
}
