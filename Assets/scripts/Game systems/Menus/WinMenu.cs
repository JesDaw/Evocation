using UnityEngine;
using UnityEngine.Events;


public class WinMenu : MonoBehaviour
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
        defeatMenuUI.SetActive(false);
        victoryMenuUI.SetActive(true);

        if(TryGetComponent<LossMenu>(out LossMenu loss))
        {
            loss.enabled = false;
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
