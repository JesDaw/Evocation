using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using System;
using UnityEngine.SceneManagement;


public abstract class PlayerHealthbarTestBase
{
    protected PlayerHealthbar componentUnderTest;
    protected MockSlider mySlider;
    protected ActivePlayer myPlayer;

    protected GameObject myHealthBar;
    protected GameObject myPlayerStorage;

    protected static GameObject prefabPlayer;


    protected const string PLAYER_PREFAB_PATH = "Assets/SceneBuilding/Prefabs/Characters/Player/Player.prefab";


    protected class MockSlider
    {
        public int maxValue = 0;
        public int value = 0;
    }

    [OneTimeSetUp]
    public static void LoadPrefabs()
    {
        prefabPlayer = AssetDatabase.LoadAssetAtPath<GameObject>(PLAYER_PREFAB_PATH);
        if (prefabPlayer == null)
        {
            throw new InvalidOperationException($"Prefab not found at path: {PLAYER_PREFAB_PATH}");
        }
    }


    [UnitySetUp]
    public IEnumerator LoadSceneObjects()
    {
        SceneManager.LoadScene("Level 1 Jesse", LoadSceneMode.Single);
        yield return null;

        Scene scene = SceneManager.GetSceneByName("Level 1 Jesse");
        GameObject playerHealthBar = GameObject.Find("PlayerHealthbar");
        GameObject activePlayerStorage = GameObject.Find("ActivePlayerStorage");

        myHealthBar = GameObject.Instantiate(playerHealthBar);
        myPlayerStorage = GameObject.Instantiate(activePlayerStorage);

        componentUnderTest = playerHealthBar.GetComponent<PlayerHealthbar>();
        myPlayer = activePlayerStorage.GetComponent<ActivePlayer>();

        mySlider = new MockSlider();
        componentUnderTest.updateHealthIndicator = (x) => mySlider.value = x;
        componentUnderTest.updateMaxHealthIndicator = (x) => mySlider.maxValue = x;

        componentUnderTest.ActivePlayer = myPlayer;

        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        componentUnderTest = null;
        myPlayer = null;

        GameObject.Destroy(myHealthBar);
        GameObject.Destroy(myPlayerStorage);

        yield return null;
    }
}

/// <summary>
/// Test cases when there is NO Active player
/// </summary>
public class NullPlayerHealthbarTest : PlayerHealthbarTestBase
{
    [SetUp]
    public void NullCurrentPlayer()
    {
        myPlayer.CurrentPlayer = null;
    }

    [UnityTest]
    public IEnumerator StartWithNullPlayerSoHealthbarIsEmpty()
    {
        Assert.IsNull(myPlayer.CurrentPlayer);
        Assert.AreEqual(0, mySlider.value);
        Assert.AreEqual(0, mySlider.maxValue);
        yield return null;
    }

    [UnityTest]
    public IEnumerator GetStatsWithNoActivePlayerReturnsNull()
    {
        Assert.IsNull(myPlayer.CurrentPlayer);
        componentUnderTest.getPlayerStats(out Stats stats);
        Assert.IsNull(stats);

        yield return null;
    }

    [UnityTest]
    public IEnumerator GetStatsWithNullPlayerReturnsNull()
    {
        componentUnderTest.getPlayerStats(null, out Stats stats);
        Assert.IsNull(stats);

        yield return null;
    }
}

/// <summary>
/// Test cases where there IS an active player
/// </summary>
public class PlayerHealthbarTest : PlayerHealthbarTestBase
{
    [SetUp]
    public void ActivatePlayer()
    {
        GameObject aPlayer = GameObject.Instantiate(prefabPlayer);
        myPlayer.CurrentPlayer = aPlayer;
    }

    [TearDown]
    public void DeactivatePlayer()
    {
        GameObject.Destroy(myPlayer.CurrentPlayer);
        myPlayer.CurrentPlayer = null;
    }

    [UnityTest]
    public IEnumerator InitialSliderMatchesPlayer()
    {
        componentUnderTest.getPlayerStats(out Stats stats);
        Assert.AreEqual(stats._CurrentHealth, mySlider.value);
        Assert.AreEqual(stats._MaxHealth, mySlider.maxValue);
        yield return null;
    }

    [UnityTest]
    public IEnumerator SliderUpdatesWithPlayerDamage()
    {
        componentUnderTest.getPlayerStats(out Stats stats);
        int initHealth = stats._CurrentHealth;
        stats.Attack(1);
        yield return new WaitForSeconds(0.1f);

        Assert.AreNotEqual(initHealth, mySlider.value);
        Assert.AreEqual(stats._CurrentHealth, mySlider.value);
        Assert.AreEqual(stats._MaxHealth, mySlider.maxValue);
        yield return null;
    }

    [UnityTest]
    public IEnumerator SliderUpdatesWithPlayerDeath()
    {
        componentUnderTest.getPlayerStats(out Stats stats);
        int initHealth = stats._CurrentHealth;
        stats.Attack(initHealth);  // Kill the player

        yield return new WaitForSeconds(0.1f);

        Assert.AreNotEqual(initHealth, mySlider.value);
        Assert.AreEqual(0, mySlider.value);
        Assert.AreEqual(stats._MaxHealth, mySlider.maxValue);

        yield return null;
    }

    [UnityTest]
    public IEnumerator GetStatsOfPlayerWithoutStatsReturnsNull()
    {
        // Remove the 'Stats' component from myPlayer
        Stats s = myPlayer.CurrentPlayer.GetComponent<Stats>();
        GameObject.Destroy(s);

        yield return null;

        componentUnderTest.getPlayerStats(myPlayer.CurrentPlayer, out Stats stats);
        Assert.IsNull(stats);

        yield return null;
    }

    [UnityTest]
    public IEnumerator DeactivatingPlayerClearsTheSlider()
    {
        myPlayer.CurrentPlayer = null;
        Assert.AreEqual(mySlider.value, 0);
        Assert.AreEqual(mySlider.maxValue, 0);
        yield return null;
    }
}