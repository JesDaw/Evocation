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
    public void OnEventRaised()
    {
        ToggleMenu.Invoke();
        pauseMenuUI.SetActive(false);
        defeatMenuUI.SetActive(true);
        victoryMenuUI.SetActive(false);

        // GameIsOver = true;

    }
}
