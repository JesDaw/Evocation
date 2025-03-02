using UnityEngine;
using UnityEngine.Events;

public class LossMenu : MonoBehaviour
{
  [SerializeField] GameObject victoryMenuUI;
  [SerializeField] GameObject defeatMenuUI;
  [SerializeField] GameObject pauseMenuUI;

  [SerializeField] UnityEvent ToggleMenu;

    void Start()
    {
        victoryMenuUI.SetActive(false);
    }
    public void LooseGame()
    {
        ToggleMenu.Invoke();
        pauseMenuUI.SetActive(false);
        defeatMenuUI.SetActive(true);
        victoryMenuUI.SetActive(false);

        if(TryGetComponent<WinMenu>(out WinMenu win))
        {
            win.enabled = false;
            Debug.Log("win menu disabled");
        }
        if(TryGetComponent<PauseMenu>(out PauseMenu pause))
        {
            pause.enabled = false;
            Debug.Log("pause menu disabled");
        }
        // GameIsOver = true;

    }
}
