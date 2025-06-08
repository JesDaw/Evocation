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
        Debug.Log("game loss");
        ToggleMenu.Invoke();
        pauseMenuUI.SetActive(false);
        defeatMenuUI.SetActive(true);
        victoryMenuUI.SetActive(false);

        if(TryGetComponent<WinMenu>(out WinMenu win))
        {
            win.enabled = false;
        }
        if(TryGetComponent<PauseMenu>(out PauseMenu pause))
        {
            pause.enabled = false;
        }
        // GameIsOver = true;

    }
}
