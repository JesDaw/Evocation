using UnityEngine;

public class LossMenu : MonoBehaviour
{
  [SerializeField] GameObject victoryMenuUI;
  [SerializeField] GameObject defeatMenuUI;
  [SerializeField] GameObject pauseMenuUI;

    void Start()
    {
        victoryMenuUI.SetActive(false);
    }
    public void OnEventRaised()
    {
      Debug.Log("here 3");
        pauseMenuUI.SetActive(false);
        defeatMenuUI.SetActive(true);
        victoryMenuUI.SetActive(false);

        // GameIsOver = true;

    }
}
