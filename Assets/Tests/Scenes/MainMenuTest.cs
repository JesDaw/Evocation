using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using UnityEditor.Build.Content;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using System;
//using UnityEngine.UIElements;



public class MainMenuTest
{
    const string MAIN_MENU_LABEL = "MainMenu";
    const string CREDITS_LABEL = "Credits";
    const string CONTROLS_LABEL = "Controls";

    const string SETTINGS_LABEL = "Settings";

    const string LEVEL_SELECT_LABEL = "LevelSelectMenu";

    /// <summary>
    /// Maps submenu names to all the Root GameObjects that should be active
    /// when that submenu is displayed
    /// </summary>
    static IDictionary<string, ISet<string>> MENU_ACTIVE_SET = new Dictionary<string, ISet<string>>()
    {
        {
            MAIN_MENU_LABEL,
            new HashSet<string>()
            {
                "New Background Image",
                MAIN_MENU_LABEL,
                "Logo",
                "CrossFade"
            }
        },
        {
            LEVEL_SELECT_LABEL,
            new HashSet<string>()
            {
                "New Background Image",
                LEVEL_SELECT_LABEL,
                "Logo",
                "CrossFade"
            }
        },
        {
            CREDITS_LABEL,
            new HashSet<string>()
            {
                "New Background Image",
                CREDITS_LABEL,
                "Logo",
                "CrossFade"
            }
        },
        {
            CONTROLS_LABEL,
            new HashSet<string>()
            {
                "New Background Image",
                CONTROLS_LABEL,
                "Logo",
                "CrossFade"
            }
        },

        {
            SETTINGS_LABEL,
            new HashSet<string>()
            {
                "New Background Image",
                SETTINGS_LABEL,
                "Logo",
                "CrossFade"
            }
        },

    };

    /// <summary>
    /// Maps submenu names to their corresponding Button name
    /// </summary>
    static IDictionary<string, string> MENU_BUTTON_FOR = new Dictionary<string, string>()
    {
        { LEVEL_SELECT_LABEL, "LevelSelectButton"},
        { CREDITS_LABEL, "CreditsButton"},
        { CONTROLS_LABEL, "ControlsButton"},
        { SETTINGS_LABEL, "SettingsButton"}
    };

    /// <summary>
    /// Enumeration of the submenu names from the MainMenu
    /// </summary>
    public static IEnumerable<string> SUBMENUS
    {
        get
        {
            return MENU_ACTIVE_SET.Keys.Where<string>(x => x != MAIN_MENU_LABEL);
        }
    }

    /// <summary>
    /// Returns if the specified menu/screen is active
    /// </summary>
    /// <param name="s">Active Scene</param>
    /// <param name="name">Menu/GameObject name</param>
    /// <returns>true if currently active, false if not</returns>
    bool isMenuEnabled(Scene s, String name)
    {
        ISet<string> enabledSet = MENU_ACTIVE_SET[name];
        GameObject[] rootObjects = s.GetRootGameObjects();

        // Cycle through all the child GameObjects directly under the
        // top Canvas and ensure that only the ones associated with
        // the 'name' menu are active/enabled
        var canvas = rootObjects.Single(x => x.name == "Canvas");
        foreach (Transform t in canvas.transform)
        {
            GameObject obj = t.gameObject;

            if (enabledSet.Contains(obj.name) ^ obj.activeInHierarchy)
            {
                Debug.Log($"{obj} has an unexpected active state!");
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Click an active button by name
    /// </summary>
    /// <param name="scene">Active Scene</param>
    /// <param name="name">Name of the GameObject containing the Button Component</param>
    /// <exception cref="InvalidOperationException">Raised with the target button is not active and enabled</exception>
    void clickButton(Scene scene, string name)
    {
        GameObject menuObj = getActiveMenu(scene);
        GameObject buttonObj = menuObj.transform.Find(name).gameObject;
        Button button = buttonObj.GetComponent<Button>();
        if (button.isActiveAndEnabled)
        {
            button.onClick.Invoke();
        }
        else
        {
            throw new InvalidOperationException($"Error attempting to click button {name}");
        }
    }

    /// <summary>
    /// Returns the GameObject of the 'screen' that is currently active on
    /// the MainMenu Canvas
    /// </summary>
    /// <param name="s">Active 'MainMenu' Scene</param>
    /// <returns>GameObject of the currently active menu/screen</returns>
    GameObject getActiveMenu(Scene s)
    {
        GameObject[] rootObjects = s.GetRootGameObjects();

        var canvas = rootObjects.Single(x => x.name == "Canvas");
        foreach (Transform t in canvas.transform)
        {
            GameObject obj = t.gameObject;

            if (MENU_ACTIVE_SET.ContainsKey(obj.name) && obj.activeInHierarchy)
            {
                return obj;
            }
        }

        return null;
    }

    [UnityTest]
    public IEnumerator IsInitialScene()
    {
        // Ensure that 'MainMenu' is the first scene by verifying
        // it is the first one in the build settings.
        EditorBuildSettingsScene scene0 = EditorBuildSettings.scenes[0];
        Assert.AreEqual(Path.GetFileNameWithoutExtension(scene0.path), "MainMenu");
        yield return null;
    }

    [UnityTest]
    public IEnumerator TopMenuIsInitiallyDisplayed()
    {
        SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
        yield return null;
        Scene scene = SceneManager.GetSceneByName("MainMenu");

        // Double-checking that this scene is the one initially displayed
        Assert.AreEqual(0, scene.buildIndex);

        Assert.IsTrue(isMenuEnabled(scene, MAIN_MENU_LABEL), "MainMenu is not displayed!");

        yield return SceneManager.UnloadSceneAsync("MainMenu");
    }

    [UnityTest]
    public IEnumerator clickToSubmenuAndBackFromSubmenu([ValueSource(nameof(SUBMENUS))] string name)
    {
        // This test case cycles through all buttons on the MainMenu to
        // verify that they properly transition to the appropriate submenu
        // when pressed.

        SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
        yield return null;
        Scene scene = SceneManager.GetSceneByName("MainMenu");

        // Click the button to enter submenu 'name'
        clickButton(scene, MENU_BUTTON_FOR[name]);
        yield return new WaitForSeconds(0.1f);

        // Verify that we are in the desired submenu/screen
        Assert.IsTrue(isMenuEnabled(scene, name), $"Failed the transition to {name}");

        // Click 'Back' button to return to top menu
        clickButton(scene, "BackButton");
        yield return new WaitForSeconds(0.1f);

        // Verify that we have returned
        Assert.IsTrue(isMenuEnabled(scene, MAIN_MENU_LABEL), "Failed to return back to the MainMenu");

        yield return SceneManager.UnloadSceneAsync("MainMenu");
    }
}
